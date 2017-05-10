AudioVideoPlayerBot is a sample consuming the AudioVideoFramePlayer class.

The AudioVideoFramePlayer class can be used if the app can provide a little ahead the list of buffers to stream.
This class will handle audio and video synchronization from the timestamp in the audio/video buffers.

Example:

Once the audio/video sockets are created, and the send status event is raised on both sockets,
you can create the AudioVideoFramePlayer and attach it to the sockets:

// here we are setting the audio buffer size to 20ms and 1sec of minimum media content,
// once the enqueued video or audio length is under a second, the player will raise a LowOnFrameEvent
_audioVideoFramePlayer = new AudioVideoFramePlayer(_audioSocket, _videoSocket,
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000));
					
// we subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames += OnLowInFrames;

// This will enqueue the audio and video buffers. The player will then stream audio and video in the background.
await _audioVideoFramePlayer.EnqueueBuffersAsync(_audioMediaBuffers, _videoMediaBuffers);


// un-subscribe to the LowOnFrameEvent
 _audioVideoFramePlayer.LowOnFrames -= OnLowInFrames;

 //shutdown the player to release all resources and free remaining enqueued buffers
await _audioVideoFramePlayer.ShutdownAsync();
