const STARWATCH_OPCODE = {
		Close        : 0,
		Hello        : 1,
		Welcome      : 2,
		Filter       : 3,
		FilterAck    : 4,
		Heartbeat    : 5,
		HeartbeatAck : 6,
	
		LogEvent     : 10,
		ServerEvent  : 12,
		PlayerEvent  : 14
	}
	
const STARWATCH_LOGLEVEL = {
	Info	: 0,
	Warning : 1,
	Error 	: 2,
	Chat 	: 3
}

class StarwatchClient
{

    constructor(server, port, channel, secure = false) 
    {
		self = this;
        this.server = server;
        this.port = port;
        this.channel = channel;
        this.isSecured = secure;
		this.allowLog = true;
		
		this.rateLimitEmbago = null;
		this.rateLimitTimeout = 0;
		this._sequence = 0;
        this._socket = null;
        this.isConnected = false;
        this.isReady = false;
		this.players = {};
		this.settings = null;
		
		this.onConnect 				= function() {}
		this.onReady 				= function() {}
		this.onClose 				= function(code, reason) {}

		this.onPlayersSync 			= function(players) { this._log("OnPlayerSync: " + players.length); }
		this.onPlayerConnected 		= function(player) { this._log("OnPlayerConnected: " + player.Username); }
		this.onPlayerDisconnected 	= function(player) { this._log("OnPlayerDisconnected: " + player.Username); }
		this.onPlayerUpdated 		= function(player) { this._log("OnPlayerUpdated: " + player.Username); }
		this.onLogMessage 			= function(log) { this._log("OnLog: " + log.Content); }
		this.onServerStart 			= function() { this._log("OnServerStart"); }
		this.onServerExit 			= function() { this._log("OnServerExit"); }
		this.onServerReload 		= function() { this._log("OnServerReload"); }
		this.onSettingsUpdate		= function(settings) { this._log("OnSettingsUpdate"); }
		this.onRateLimitReached		= function(limit) { console.error(limit); }
	}

    connect() 
    {

        //Connect to the websocket
        let protocal = this.isSecured ? "wss://" : "ws://";
        let url = protocal + this.server + ":" + this.port + "/" + this.channel;
        this._socket = new WebSocket(url);
        this._socket.binaryType = "arraybuffer";

        //Listen to events
        this._log('Socket Status: ' + this._socket.readyState);
		this._socket.onopen = (event) => 
		{ 
			//Send the hello
			this.isConnected = true;
			this._send(new Frame(this._getNextSequence(), STARWATCH_OPCODE.Hello, "HELO", JSON.stringify( { Agent: "StarwatchJS/1.0 (offical)"})));
			this.onConnect();
			this._reloadSettings();
		}

		this._socket.onclose = (event) => 
		{
			this._log("Socket has closed!"); 
			this._log(event);
			this.isConnected = false;
			this.isReady = false;
            this.onClose(event.code, event.reason);
            
            if (event.code == 1008)
                console.error("Forbidden from accessing the API!");
		}

		this._socket.onerror = (event) => 
		{
			console.error(event);
		}

		this._socket.onmessage = (event) => 
		{ 			
			//Read the frame
			let buff = new Frame(0, 0, "");
			buff = buff.FromBytes(event.data);
			if (buff == null) 
			{
				console.warn("Invalid buffer read. Could be a invalid version?");
				return;
			}

			//Handle the events
			switch(buff.opcode) 
			{
				default: 
					console.error("Unkown OpCode: " + buff.opcode);
					break;

				case STARWATCH_OPCODE.Welcome:					
					this.isReady = true;
					this._send(new Frame(this._getNextSequence(), STARWATCH_OPCODE.Filter, "ENBL"));
					this.syncPlayers();
					this.onReady();
					break;

				case STARWATCH_OPCODE.LogEvent:
					this.onLogMessage(buff.object);
					break;

				case STARWATCH_OPCODE.PlayerEvent:
					if (buff.identifier == "UPDT") { this._pushPlayer(buff.object, this.onPlayerUpdated); }
					if (buff.identifier == "CONN") { this._pushPlayer(buff.object, this.onPlayerConnected); }
					if (buff.identifier == "DISC") { delete this.players[buff.object.Connection]; this.onPlayerDisconnected(buff.object); }
					if (buff.identifier == "SYNC") 
					{ 
						this.players = {};
						for(var i = 0; i < buff.object.length; i++) this._pushPlayer(buff.object[i], null);
						this.onPlayersSync(buff.object);
					}
					break;

				case STARWATCH_OPCODE.ServerEvent:
					if (buff.identifier == "LOAD")  
					{
						this.onServerReload();
						this._reloadSettings();
					}

					if (buff.identifier == "STRT") 
					{
						this.onServerStart();
						this._reloadSettings();
					}

					if (buff.identifier == "EXIT") 
					{
						this.onServerExit(); 
						this.onPlayersSync([]); 
					}

					break;

				case STARWATCH_OPCODE.FilterAck:
					this._log("Filter Acknowledgement: " + buff.content);
					break;
			}
		}
		
        this.isConnected = false;
        this.isReady = false;
	}

	syncPlayers() 
	{
		this._log("Syncing Players...");
		this._send(new Frame(this._getNextSequence(), STARWATCH_OPCODE.Filter, "SYNC"));
	}

	_pushPlayer(player, callback) 
	{
		let id = player.Connection;
		this.players[id] = player;
		this.players[id].starwatch = this;
		this.players[id].kick = function(reason)  { this.starwatch.kickPlayer(this.Connection, reason); }
		this.players[id].timeout = function(reason, duration)  { this.starwatch.timeoutPlayer(this.Connection, reason, duration); }
		this.players[id].ban = function(reason)  { this.starwatch.banPlayer(this.Connection, reason); }
		if (callback != null) callback(this.players[id]);
	}

	broadcast(message) 
	{
		this._log("Broadcasting " + message);
		this._rest("POST", "/chat?async&include_tag", { Content: message });
	}
	
	restartServer() 
	{
		this._log("Restarting Server");
        this._rest("DELETE", "/server");
	}

	timeoutPlayer(cid, reason, time) 
	{
		this._log("Timingout " + cid + " because " + reason);
		this._rest("DELETE", "/player/"+cid+"?reason=" + encodeURIComponent(reason) + "&duration=" + time);
	}
	kickPlayer(cid, reason) 
	{
		this._log("Kicking " + cid + " because " + reason);
		this._rest("DELETE", "/player/"+cid+"?reason=" + encodeURIComponent(reason));
	}
	banPlayer(cid, reason)
	{
		this._log("Banning " + cid + " because " + reason);
		this._rest("POST", "/ban?cid=" + cid, { reason: reason });
	}

	getStatistics(callback = function(data) {})
	{
		this._log("Getting statistics...");
		this._rest("GET", "/server/statistics", null, callback);
	}

	patchSettings(patch)
	{
		this._log("Patching Settings...");		
		this._rest("PUT", "/server", patch, function(data) { self._reloadSettings(); });
	}
	getSettings(callback = function(data) {})
	{
		this._log("Getting settings...");
		this._rest("GET", "/server", null, callback);
	}

	_reloadSettings() 
	{
		this.getSettings(function(data) {
			if (data.Status == 0) 				
			{			
				self.settings = data.Response; 
				self.onSettingsUpdate(self.settings); 
			}
			else
			{
				console.error(data.Message);
			}
		});
	}
	_getNextSequence() { return this._sequence = (this._sequence + 1 >= 65535 ? 0 : this._sequence + 1); }
	_rest(method, endpoint, data = null, response = function(res) {}) 
	{
		this._log("REST: " + method + " " + endpoint);
		if (self.rateLimitEmbago != null)
		{
			if ((new Date()) < this.rateLimitEmbago)
			{
				console.error("Failed to execute REST as the ratelimit embago is still active!");
				return;
			}
			else
			{
				console.log("Ratelimit Embago finished!");
				this.rateLimitEmbago = null;
				this.rateLimitTimeout = 0;
			}
		}

		let protocal = this.isSecured ? "https://" : "http://";
		let url = protocal + this.server + ":" + this.port + "/api";

		$.ajax({
			url: url + endpoint,
			type: method,
			data: data != null ? JSON.stringify(data) : null,
			contentType: data != null ? "application/json" : null,
			error: function(result) 
			{ 
				if (result.responseJSON.Status == 4290)
				{
					console.log(result.responseJSON.Response);
					console.error("Connection has been ratelimited!");
					self.rateLimitEmbago = new Date(result.responseJSON.Response.RetryAfter);
					self.rateLimitTimeout = self.rateLimitEmbago.getTime() - new Date().getTime();
					self.onRateLimitReached({
						embago: self.rateLimitEmbago,
						timeout: self.rateLimitTimeout,
						limit: result.responseJSON.Response.Limit,
						remaining: result.responseJSON.Response.Remaining
					});
				}
				else
				{
					console.error(result.responseJSON);
				}
			},
			success: response
		});
	}
	_send(frame) {
		var bytes = frame.ToBytes();		
		this._socket.send(bytes);
	}

	_log(message) {
		if (!this.allowLog) return;
		console.log(message);
	}
}

class Frame
{
	constructor(sequence, opcode, identifier, content = "") 
	{
		this.version = 5;
		this.sequence = sequence;
		this.opcode = opcode;
		this.identifier = identifier;
		this.content = content;
		this.object = null;
	}

	ToBytes() 
	{
		let bsw = new ByteStreamWriter();
		bsw.writeByte(this.version);
		bsw.writeUShort(this.sequence);
		bsw.writeByte(this.opcode);
		bsw.writeChars(this.identifier);
		bsw.writeString(this.content);
		bsw.writeByte(0); bsw.writeByte(0);
		bsw.writeByte(0); bsw.writeByte(0);
		return bsw.getBuffer();
	}

	FromBytes(bytes) 
	{
		//Create the reader
		let bsr = new ByteStreamReader(bytes);

		//Check the version
		let vsr = bsr.readByte();
		if (vsr != this.version) return null;

		//Continue on
		this.sequence = bsr.readUShort();
		this.opcode = bsr.readByte();
		this.identifier = bsr.readChars(4);
		this.content = bsr.readString();
		this.object = JSON.parse(this.content);

		bsr.readByte(); bsr.readByte();
		bsr.readByte(); bsr.readByte();
		return this;
	}
}