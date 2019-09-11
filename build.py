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

if build_config["Version"] == "NET35":
    build_ver = "v3.5"
elif build_config["Version"] == "NET45":
    build_ver = "v4.5.2"

marauder_settings = dict({
    "PayloadName": build_config["PayloadName"],
    "Password": build_config["PayloadKey"],
    "InitialTransportType": build_config["InitialTransportType"],
    "TransportModule": build_config["TransportModule"],
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

restore_cmd = "nuget restore"
print("[Marauder Build] Running restore command: {}".format(restore_cmd))
restore_exit = call(restore_cmd, shell=True)

if restore_exit == 0:
    # Setup version
    if build_config["Version"] == "NET35":
        version = "v3.5"
    elif build_config["Version"] == "NET45":
        version = "v4.5.2"
    else:
        print("[Marauder Build] Could not find a match for version: {}".format(build_config["Version"]))
        sys.exit(1)

    # Setup Debug vs Release and build
    if configuration == "Debug":
        build_cmd = "msbuild Marauder.csproj /t:Build /p:Configuration=Debug /p:OutputType=exe /p:TargetFrameworkVersion={}".format(version)
    else:
        build_cmd = "msbuild Marauder.csproj /t:Build /p:Configuration=Release /p:Optimize=true /p:OutputType=Winexe /p:TargetFrameworkVersion={}".format(version)
    print("[Marauder Build] Running build command: {}".format(build_cmd))
    build_exit = call(build_cmd, shell=True)
else:
    print("[Marauder Build] Failed to restore packages.")
    sys.exit(1)

if build_exit == 0:
    shutil.move(output_path, "./output/Marauder.exe")
else:
    print("[Marauder Build] Build Failed.")
    sys.exit(1)

