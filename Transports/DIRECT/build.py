import sys
import json

try:
    with open(sys.argv[1]) as build_file:  
    build_config = json.load(build_file)
except Exception as e:
    print("Could not load build config file. Error {}".format(e.Message))
    sys.exit(1)

restore_cmd = "nuget restore ./Transports/DIRECT/packages.config -PackagesDirectory ./Transports/DIRECT/packages"



_settings = dict({
    "PayloadName": build_config["PayloadName"],
    "Password": build_config["PayloadKey"],
    "Transport": build_config["Transport"],
    "BeaconInterval": build_config["BeaconInterval"],
    "Jitter": build_config["Jitter"],
    "ExpirationDate": build_config["ExpirationDate"],
    "Debug": build_config["Debug"]
})

if build_config["Debug"]:
    configuration = "Debug"
else:
    configuration = "Release"

build_cmd = "mcs -pkg:dotnet -t:library -r:./Transports/DIRECT/packages/Newtonsoft.Json.12.0.1/lib/net35/Newtonsoft.Json.dll -r:./Transports/DIRECT/packages/Faction.Modules.Dotnet.Common.20190309.0.0/lib/net35/Faction.Modules.Dotnet.Common.dll -out:./Transports/DIRECT/DIRECT.dll ./Transports/DIRECT/DIRECT-build.cs"