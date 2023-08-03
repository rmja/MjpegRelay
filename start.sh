#!/bin/bash

#dotnet MjpegRelay.dll --urls=http://+:9000 --source=rtsp --rtsp:streamurl=rtsp://camera/stream

i=1
while true
do
  url="url_$i"
  url=${!url}
	url=${url,,}
	port=$((9000 + i))
	if [[ -z "$url" ]]; then
	  break
	else
	  if [ "${url:0:4}" = "rtsp" ]; then
		  echo "$i rstp"
			if [[ ! -z "$url" ]]; then
			  dotnet MjpegRelay.dll --urls=http://+:$port --source=rtsp --rtsp:streamurl=$url &
			fi
		elif [ "${url:0:5}" = "mjpeg" ]; then
		  echo "$i mjpeg"
			if [[ ! -z "$url" ]]; then
			  dotnet MjpegRelay.dll --urls=http://+:$port --source=rtsp --mjpeg:streamurl=$url &
			fi
		else
		  break
		fi
    i=$((i+1))
	fi
done

while true
do
	sleep 120
done

