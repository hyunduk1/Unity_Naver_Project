var WSConnection =
{
	// Connection list (singleton):
	$webSocketPool: [],
	
	// Create a new connection (linking to static events in C# side):
	_NewWebSocket: function(onOpen, onMessage, onError, onClose)
	{
		// Search for a disposed ID before assign a new one.
		var id = webSocketPool.length;
		for(i = 0; i < webSocketPool.length; i++)
		{
			if(webSocketPool[i] === null)
			{
				id = i;
				break;
			}
		}
		// Set the new WebSocket:
		webSocketPool[id] = {
			_ws: null,
			_onOpen: onOpen,
			_onMessage: onMessage,
			_onError: onError,
			_onClose: onClose
		};
		return id;
	},
	
	// Establish the connection:
	WSConnect: function(id, url)
	{
		if(webSocketPool[id]._ws === null)
		{
			webSocketPool[id]._ws = new WebSocket(Pointer_stringify(url));
			webSocketPool[id]._ws.binaryType = 'arraybuffer';
			webSocketPool[id]._ws.onopen = function()
			{
				Runtime.dynCall('vi', webSocketPool[id]._onOpen, [ id ]);
			};
			webSocketPool[id]._ws.onmessage = function(event)
			{
				if (event.data instanceof ArrayBuffer)
				{
					var data = new Uint8Array(event.data);
					var buffer = _malloc(data.length);
					HEAPU8.set(data, buffer);
					try
					{
						Runtime.dynCall('viii', webSocketPool[id]._onMessage, [ id, buffer, data.length ]);
					} finally {
						_free(buffer);
					}
				}
			};
			webSocketPool[id]._ws.onerror = function(event)
			{
				Runtime.dynCall('vii', webSocketPool[id]._onError, [ id, event.data ]);
			};
			webSocketPool[id]._ws.onclose = function(event)
			{
				Runtime.dynCall('vii', webSocketPool[id]._onClose, [ id, event.code ]);
				webSocketPool[id]._ws = null;
			};
		}
	},
	
	// Send data:
	WSSend: function (id, data, length)
	{
		// Check if WebSocket is open:
		if(webSocketPool[id]._ws !== null && webSocketPool[id]._ws.readyState == 1)
		{
			webSocketPool[id]._ws.send(HEAPU8.buffer.slice(data, data + length));
		}
	},
	
	// Close connection:
	WSDisconnect: function(id)
	{
		if( webSocketPool[id]._ws !== null)
		{
			webSocketPool[id]._ws.onopen = null;
			webSocketPool[id]._ws.onmessage = null;
			webSocketPool[id]._ws.onerror = null;
			webSocketPool[id]._ws.onclose = null;
			webSocketPool[id]._ws.close();
			webSocketPool[id]._ws = null;
		}
	},
	
	// Dispose connection:
	WSDispose: function(id)
	{
		if(webSocketPool[id]._ws !== null)
		{
			webSocketPool[id]._ws.onopen = null;
			webSocketPool[id]._ws.onmessage = null;
			webSocketPool[id]._ws.onerror = null;
			webSocketPool[id]._ws.onclose = null;
			webSocketPool[id]._ws.close();
			webSocketPool[id]._ws = null;
		}
		webSocketPool[id] = null;
	},

	// Return the current URL:
	WSGetUrl: function(id)
	{
		var returnStr = '';
		if(webSocketPool[id]._ws !== null)
			returnStr = webSocketPool[id]._ws.url;
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	},
	
	// Returns the WebSocket state:
	WSGetState: function(id)
	{
		if(webSocketPool[id]._ws !== null)
			return webSocketPool[id]._ws.readyState;
		else
			return 3;
	}
}
autoAddDeps(WSConnection, '$webSocketPool');		// Declare $webSocketPool as a singleton dependency.
mergeInto(LibraryManager.library, WSConnection);	// Add plugin to Unity.