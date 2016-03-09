#!/bin/bash

export targetApp=HelloWorldMvc
export workspace="${HOME}/aspnet/tests/workspace"

if [ ! -d $workspace ]; then
    mkdir -p $workspace
fi

outputDir="${workspace}/tests/current"

if [ ! -d $outputDir ]; then
    mkdir -p $outputDir
fi

outputFile=`find $outputDir -maxdepth 1 -name "Results-*.csv" | sort | tail -1`

if [ -z $outputFile ]
then
    runId=`date +%y-%m-%d-%H-%M-%S`
    echo "There is no existing run, create a new run and use timestamp ${runId} as runId"
    outputFile="${outputDir}/Results-${runId}.csv"
fi

echo "Tests will be recorded in file: $outputFile"

export outputFile

export DOTNET_PACKAGES_CACHE="${HOME}/.nuget/packages"
