import sys
import json

try:
    with open(sys.argv[1]) as build_file:  
    build_config = json.load(build_file)
except Exception as e:
    print("Could not load build config file. Error {}".format(e.Message))
    sys.exit(1)

restore_cmd = "nuget restore ./Transports/DIRECT/packages.config -PackagesDirectory ./Transports/DIRECT/packages"


if build_config["Debug"]:
    configuration = "Debug"
else:
    configuration = "Release"

build_cmd = "msbuild DIRECT.csproj"