import sys
import json
import shutil
from subprocess import call

try:
    with open(sys.argv[1]) as build_file:  
        build_config = json.load(build_file)
except Exception as e:
    print("Could not load build config file. Error {}".format(e.Message))
    sys.exit(1)

restore_cmd = "nuget restore ./Transports/DIRECT/packages.config -PackagesDirectory ./Transports/DIRECT/packages"
call(restore_cmd, shell=True)

if build_config["Debug"]:
    configuration = "Debug"
    output_path = "./Transports/DIRECT/bin/Debug/DIRECT.dll"
else:
    configuration = "Release"
    output_path = "./Transports/DIRECT/bin/Release/DIRECT.dll"

transport_config = json.load(build["TransportConfiguration"])
transport_config["Debug"] = build_config["Debug"]

with open('./settings.json', 'w') as settings_file:
   settings_file.write(transport_config)

build_cmd = "msbuild ./Transports/DIRECT/DIRECT.csproj /t:Build /p:Configuration={} /p:TargetFramework={}".format(configuration, build_config["Version"])
call(build_cmd, shell=True)

shutil.move(output_path, "./output/Transports/DIRECT.dll")