import sys
import json
import shutil
from subprocess import call

try:
    print("[HTTP Transport Build] Loading config from {}..".format(sys.argv[1]))
    with open(sys.argv[1]) as build_file:  
        build_config = json.load(build_file)
except Exception as e:
    print("[HTTP Transport Build] Could not load build config file. Error {}".format(str(e)))
    sys.exit(1)

print("[HTTP Transport Build] Restoring packages..")
restore_cmd = "nuget restore ./Transports/HTTP/packages.config -PackagesDirectory ./Transports/HTTP/packages"
call(restore_cmd, shell=True)

if build_config["Debug"]:
    configuration = "Debug"
    output_path = "./Transports/HTTP/bin/Debug/HttpTransport.dll"
else:
    configuration = "Release"
    output_path = "./Transports/HTTP/bin/Release/HttpTransport.dll"

print("[HTTP Transport Build] Creating Transport Config..")
transport_config = json.loads(build_config["TransportConfiguration"])
transport_config["Debug"] = build_config["Debug"]
print("[HTTP Transport Build] Writing config to settings.json..\n\n {}".format(transport_config))

with open('./Transports/HTTP/settings.json', 'w') as settings_file:
   settings_file.write(json.dumps(transport_config))

print("[HTTP Transport Build] Building Transport..")
build_cmd = "msbuild ./Transports/HTTP/HttpTransport.csproj /t:Build /p:Configuration={} /p:TargetFramework={}".format(configuration, build_config["Version"])
call(build_cmd, shell=True)

print("[HTTP Transport Build] Moving to ./output/Transports/HttpTransport.dll")
shutil.move(output_path, "./output/Transports/HttpTransport.dll")