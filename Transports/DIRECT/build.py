import sys
import json
import shutil
from subprocess import call

try:
    print("[DIRECT Transport Build] Loading config from {}..".format(sys.argv[1]))
    with open(sys.argv[1]) as build_file:  
        build_config = json.load(build_file)
except Exception as e:
    print("[DIRECT Transport Build] Could not load build config file. Error {}".format(str(e)))
    sys.exit(1)

print("[DIRECT Transport Build] Restoring packages..")
restore_cmd = "nuget restore ./Transports/DIRECT/packages.config -PackagesDirectory ./Transports/DIRECT/packages"
call(restore_cmd, shell=True)

if build_config["Debug"]:
    configuration = "Debug"
    output_path = "./Transports/DIRECT/bin/Debug/DIRECT.dll"
else:
    configuration = "Release"
    output_path = "./Transports/DIRECT/bin/Release/DIRECT.dll"

print("[DIRECT Transport Build] Creating Transport Config..")
transport_config = json.loads(build_config["TransportConfiguration"])
transport_config["Debug"] = build_config["Debug"]
print("[DIRECT Transport Build] Writing config to DIRECT.settings.json..\n\n {}".format(transport_config))

with open('./Transports/DIRECT/settings.json', 'w') as settings_file:
   settings_file.write(json.dumps(transport_config))

print("[DIRECT Transport Build] Building Transport..")
build_cmd = "msbuild ./Transports/DIRECT/DIRECT.csproj /t:Build /p:Configuration={} /p:TargetFramework={}".format(configuration, build_config["Version"])
call(build_cmd, shell=True)

print("[DIRECT Transport Build] Moving to ./output/Transports/DIRECT.dll")
shutil.move(output_path, "./output/Transports/DIRECT.dll")