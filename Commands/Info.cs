using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using Faction.Modules.Dotnet.Common;
using Marauder.Objects;
using Marauder.Services;

namespace Marauder.Commands
{
    class InfoCommand : Command
    {
        class SettingObject
        {
            public string Setting;
            public string Value;
        }
        
        public override string Name { get { return "info"; } }
        public override CommandOutput Execute(Dictionary<string, string> Parameters = null)
        {
            CommandOutput output = new CommandOutput {Complete = true, Success = true};
            
            List<SettingObject> settings = new List<SettingObject>();
            
            settings.Add(new SettingObject {Setting = "BeaconInterval", Value = State.Sleep.ToString() });
            settings.Add(new SettingObject {Setting = "Name", Value = State.Name });
            settings.Add(new SettingObject {Setting = "Jitter", Value = State.Jitter.ToString("##0.0#") });
            settings.Add(new SettingObject {Setting = "MaxAttempts", Value = State.MaxAttempts.ToString() });
            settings.Add(new SettingObject {Setting = "RunningTaskCount", Value = State.RunningTasks.Count.ToString()});
            settings.Add(new SettingObject {Setting = "Debug", Value = State.Debug.ToString() });
            if (State.ExpirationDate.HasValue)
            {
                settings.Add(new SettingObject {Setting = "ExpirationDate (UTC)", Value = State.ExpirationDate.Value.ToUniversalTime().ToString("o")});
            }
            else
            {
                settings.Add(new SettingObject {Setting = "ExpirationDate (UTC)", Value = "None"});
            }
            
            output.Message = JsonConvert.SerializeObject(settings);
            
            return output;
        }
    }
}