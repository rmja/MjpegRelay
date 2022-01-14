# Motion JPEG Relay
This is yet another implementation of a mjpeg relay/forwarder/proxy.
This time in C#.

It makes a single connection to a camera stream (either rtsp or mjpeg),
and provides a stream endpoint which can be used for many clients without any burdon on the camera.

It has the following command line switches:

* `--source` must be either `rtsp` or `mjpeg`.
* `--rtsp:streamurl` the url of the rtsp source stream.
* `--rtsp:transport` the rtsp transport method, default is `tcp`.
* `--mjpeg:streamurl` the url of the mjpeg source stream.

An example is
```
dotnet run -- --source=rtsp --rtsp:streamurl=rtsp://camera/stream
```
This will start a server on port `5000` and the relayed stream is available at http://localhost:5000/stream.

## Docker
The program is readily available on [docker kub](https://hub.docker.com/r/rmjac/mjpeg-relay).
The same command line switches apply, for example:
```
docker run --network=host rmjac/mjpeg-relay --urls=http://+:9000 --source=rtsp --rtsp:streamurl=rtsp://some/stream
```

This will start a server on host running on port 9000 that connects to the rtsp stream and serves a relayed mjpeg stream on http://localhost:9000/stream.

## Build Instructions

```
docker build -t rmjac/mjpeg-relay -f src\MjpegRelay\Dockerfile .
docker push rmjac/mjpeg-relay
```
