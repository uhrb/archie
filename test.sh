#!/bin/bash
dotnet test --logger "html;verbosity=detailed;logfilename=report.html" /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info 