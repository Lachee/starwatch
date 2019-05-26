class Starwatch
{
    constructor(server, port, secure = false) 
    {
        this.server = server;
        this.port = port;
        this.secure = secure;
        this.url = (secure ? "wss://" : "ws://") + this.server + ":" + this.port;
    
        this.logConnected = false;
        this.eventConnected = false;

		this.rateLimitEmbago = null;
        this.rateLimitTimeout = 0;

        this._logws = null;
        this._evtws = null;

        this.players = {};
        this._events = {};
        
        this.onPlayerSync = function(players) {}
        this.onPlayerJoin = function(player) {}
        this.onPlayerLeave = function(player) {}
        this.onPlayerUpdate = function(player) {}

        this.onRateLimitReached = function(ratelimit) { console.error("REST: RATELIMIT", ratelimit); }

        this.onEventConnected = function() {}
        this.onEventDisconnected = function(reason) {}

        this.onLogConnected = function() {}
        this.onLogDisconnected = function(reason) {}

        let self = this;
        this.on("OnCallback", function(evnt) {
            switch(evnt._route)
            {
                default: break;
                case "/player/all":
                    self.players = [];
                    for (var key in evnt) 
                    { 
                        if (!evnt.hasOwnProperty(key)) continue;
                        if (key.startsWith("_")) continue;
                        self._storePlayer(evnt[key]);
                    }
                    console.log(self.players);
                    self.onPlayerSync(self.players);
                    break;
            }
        });

        this.on("OnPlayerConnect", function(p) { 
            console.log("PLAYER CONNECT");
            self._storePlayer(p);
            self.onPlayerJoin(p);
        });
        this.on("OnPlayerDisconnect", function(p) {
            console.log("PLAYER LEAVE");
            delete self.players[p.Connection];
            self.onPlayerLeave(p);
        });
        this.on("OnPlayerUpdate", function(p) {            
            self._storePlayer(p);
            self.onPlayerUpdate(p);
        });
        this.on("OnServerExit", function(p) {
            self.players = [];
            self.onPlayerSync([]);
        });
    }
    
    broadcast(message) { this.request("POST", "/chat?async&include_tag", { Content: message }); }
    restart()  { this.request("DELETE", "/server"); }

    ban(connection, reason)
    { 
        this.request("POST", `/ban?cid=${connection}`, { reason: reason });
    }
    timeout(connection, reason, duration) 
    {
        this.request("DELETE", `/player/${connection}?reason=${encodeURIComponent(reason)}&duration=${duration}`);
    }
    kick(connection, reason) 
    {
        this.request("DELETE", `/player/${connection}?reason=${encodeURIComponent(reason)}`);
    }
    fetchStatistics(success = null, error = null) { this.request("GET", "/server/statistics", null, success, error); }

    fetchSettings(success = null, error = null) { this.request("GET", "/server", null, success, error); }
    patchSettings(settings, async = false, success = null, error = null) { this.request("PUT", `/server?async=${async}`, settings, success, error); }

    close()
    {
        this._evtws.close();
        this._logws.close();
    }
    connect() 
    {
        if (!this.eventConnected) 
        {
            this._evtws = new WebSocket(this.url + "/events");
            this._evtws.onerror = (e) => console.error("WS: EVENT", e);
            this._evtws.onopen = (e) => 
            {                 
                this.eventConnected = true;
                this.onEventConnected();
                this._evtws.send(JSON.stringify({ Endpoint: "/player/all" }));
            }

            this._evtws.onclose = (e) => {       
                console.warn("WS: EVENT CLOSED", e);        
                this.eventConnected = false;
                this.onEventDisconnected(e);
            }

            this._evtws.onmessage = (e) => {
                let obj = JSON.parse(e.data);
                let res = obj.Response;
                res._route = obj.Route;
                res._event = obj.Event;
                console.log("EVT", res._event, res);
                this._invoke("event", res);
                this._invoke(obj.Event, res);
            }
        }

        if (!this.logConnected) 
        {
            this._logws = new WebSocket(this.url + "/log");
            this._logws.onerror = (e) => console.error("WS: LOGS", e);
            this._logws.onopen = (e) => {                 
                this.logConnnected = true;
                this.onLogConnected();
            }

            this._logws.onclose = (e) => {     
                console.warn("WS: LOGS CLOSED", e);          
                this.logConnnected = false;
                this.onLogDisconnected(e);
            }

            this._logws.onmessage = (e) => this._invoke("log", e.data);
        }
    }

    on(event, callback) 
    {
        if (this._events[event] == null) 
            this._events[event] = [];

        this._events[event].push(callback);
    }

    _storePlayer(player)
    {
        let id = player.Connection;
        this.players[id] = player;
        this.players[id].starwatch = this;
        this.players[id].kick = function(reason)  { this.starwatch.kick(this.Connection, reason); }
		this.players[id].timeout = function(reason, duration)  { this.starwatch.timeout(this.Connection, reason, duration); }
		this.players[id].ban = function(reason)  { this.starwatch.ban(this.Connection, reason); }
    }
    _invoke(event, data) 
    {
        if (this._events[event] != null)
            this._events[event].forEach(function(e) { e(data); });        
    }

    request(method, endpoint, data = null, success = null, error = null)
    {
        if (success == null) success = function(evt) {};
        if (error == null) error = function(evt) {};
        if (this.rateLimitEmbago != null)
		{
			if ((new Date()) < this.rateLimitEmbago)
			{
				console.error("REST: Failed to execute REST as the ratelimit embago is still active!");
				return;
			}
			else
			{
				console.log("REST: Ratelimit Embago finished!");
				this.rateLimitEmbago = null;
				this.rateLimitTimeout = 0;
			}
        }
        
        var xhr = new XMLHttpRequest();
        var self = this;
        xhr.onload = function()
        {
            let res = JSON.parse(xhr.responseText);
            if (xhr.status >= 200 && xhr.status < 300)
            {
                success(res.Response);
            }
            else
            {
                if (res.Status == 4290)
                {
                    console.warn("REST: Connection Ratelimited", res);
                    self.rateLimitEmbago = new Date(res.Response.RetryAfter);
					self.rateLimitTimeout = self.rateLimitEmbago.getTime() - new Date().getTime();
					self.onRateLimitReached({
						embago: self.rateLimitEmbago,
						timeout: self.rateLimitTimeout,
						limit: res.Response.Limit,
						remaining: res.Response.Remaining
					});
                }
                
                console.error("REST: ERROR", res);
                error(res);
            }
        };

		let protocal = this.secure ? "https://" : "http://";
		let url = protocal + this.server + ":" + this.port + "/api" + endpoint;
        xhr.open(method, url);
        xhr.setRequestHeader("content-type", "application/json");

        if (data == null)  xhr.send();
        else xhr.send(JSON.stringify(data));
    }
}