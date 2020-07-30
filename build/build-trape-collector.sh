#!/bin/bash

BASEDIR=$(readlink -f $(dirname $0))
SOURCEDIR=$BASEDIR/../src
DEBIANDIR=$BASEDIR/../src/package/collector/DEBIAN
CURRENTPROJECT=

function error()
{
	echo
	echo "FAILED in ${CURRENTPROJECT}"
	exit 1
}

trap error ERR

echo "Building Trape Collector"
CURRENTPROJEFT="Trape Collector"
PROFILE=$1

if [ -z $PROFILE ]; then
	PROFILE=Debug
elif [[ "d" == "$PROFILE" ]]; then
	PROFILE=Debug
elif [[ "r" == "$PROFILE" ]]; then
	PROFILE=Release
else
	echo "Provided argument '$1' was not understood"
	exit 1
fi

echo "Building Profile: $PROFILE"

cd $SOURCEDIR/trape/Trape.Cli.Collector

# Building
dotnet-sdk.dotnet clean
dotnet-sdk.dotnet build --configuration:${PROFILE} --runtime:ubuntu.18.04-x64

cd $BASEDIR

echo "Packing Trape Collector"
cd $BASEDIR

# Creating directory structure

VERSION=0.1-1
PACKINGDIR=$BASEDIR/packaging/trape-collector_$VERSION
TARGETDIR=$PACKINGDIR/opt/trape/collector/
METADIR=$PACKINGDIR/DEBIAN

rm -rf $PACKINGDIR
rm -rf trape-trader_$VERSION.deb
mkdir -p $TARGETDIR
mkdir -p $METADIR
cp -r $SOURCEDIR/trape/Trape.Cli.Collector/bin/Debug/netcoreapp3.1/ubuntu.18.04-x64/* $TARGETDIR
chmod 644 $TARGETDIR/*
chmod 744 $TARGETDIR/Trape.Cli.Collector

# Prepare package meta information
cp $DEBIANDIR/* $METADIR/
sed -i 's/\$VERSION/'$VERSION'/' $METADIR/control

cd $BASEDIR/packaging
dpkg-deb -v --build trape-collector_$VERSION


