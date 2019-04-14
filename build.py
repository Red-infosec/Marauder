import sys
import json

try:
    with open(sys.argv[1]) as build_file:  
    build_config = json.load(build_file)
except Exception as e:
    print("Could not load build config file. Error {}".format(e.Message))
    sys.exit(1)

marauder_settings = dict({
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

build_cmd = "msbuild Marauder.csproj /p:TrimUnusedDependencies=true /t:Build /p:Configuration={} /p:TargetFramework={} /p:Platform={}".format(configuration, build_config["Version"], build_config["Architecture"])