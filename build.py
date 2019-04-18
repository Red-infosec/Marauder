import sys
import json
import shutil
from subprocess import call

print("[Marauder Build] Loading JSON file from {}".format(sys.argv[1]))
try:
    with open(sys.argv[1]) as build_file:  
        build_config = json.load(build_file)
except Exception as e:
    print("[Marauder Build] Could not load build config file. Error {}".format(e.Message))
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

print("[Marauder Build] Writing agent config values to ./settings.json")
with open('./settings.json', 'w') as settings_file:
    json.dump(marauder_settings, settings_file)

if build_config["Debug"]:
    configuration = "Debug"
    output_path = "./bin/Debug/Marauder.exe"
else:
    configuration = "Release"
    output_path = "./bin/Release/Marauder.exe"

build_cmd = "msbuild Marauder.csproj /p:TrimUnusedDependencies=true /t:Build /p:Configuration={} /p:TargetFramework={}".format(configuration, build_config["Version"])

print("[Marauder Build] Running build command: {}".format(build_cmd))
call(build_cmd, shell=True)

shutil.move(output_path, "./output/Marauder.exe")

