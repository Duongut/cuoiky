#!/bin/bash

cd /home/user/ProjectITS/SmartParking.Core/SmartParking.Core

# Compile and run the FixDuplicateVehiclesProgram
dotnet run --project . --no-build FixDuplicateVehiclesProgram.cs
