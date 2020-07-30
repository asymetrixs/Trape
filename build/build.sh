#!/bin/bash

BASEDIR=$(readlink -f $(dirname $0))
CURRENTPROJECT=

function error()
{
	echo
	echo "FAILED in ${CURRENTPROJECT}"
	exit 1
}

trap error ERR

PROFILE=$1

if [ -z $PROFILE ]; then
	PROFILE=d
elif [[ "d" == "$PROFILE" ]]; then
	PROFILE=d
elif [[ "r" == "$PROFILE" ]]; then
	PROFILE=r
else
	echo "Provided argument '$1' was not understood"
	exit 1;
fi

echo "Building Profile: $PROFILE"

$BASEDIR/build-trape-collector.sh $PROFILE

$BASEDIR/build-trape-trader.sh $PROFILE
