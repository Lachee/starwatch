class ByteStreamWriter 
{
	constructor() 
	{
		this._tuffer = new Uint8Array(0);
	}
	
	//====== INT
	writeInt(value) {				
		var tarr = new Int32Array([value]);
		this._addArray(tarr);
	}
	writeUInt(value) {				
		var tarr = new Uint32Array([value]);
		this._addArray(tarr);
	}
	
	//====== SHORT
	writeShort(value) {				
		var tarr = new Int16Array([value]);
		this._addArray(tarr);
	}
	writeUShort(value) {				
		var tarr = new Uint16Array([value]);
		this._addArray(tarr);
	}
	
	//======= BYTE
	writeByte(value) {				
		var tarr = new Int8Array([value]);
		this._addArray(tarr);
	}
	writeUByte(value) {				
		var tarr = new Uint8Array([value]);
		this._addArray(tarr);
	}
	
	//========= BYTES
	writeBytes(bytes) {
		var tarr = new Uint8Array(bytes);
		this.writeUBytes(tarr);
	}
	writeUBytes(bytes) {
		this.writeInt(bytes.byteLength);
		if (bytes.byteLength == 0) return;
		
		//Prepare its ofset and enlargen
		var offset = this._tuffer.byteLength;
		this._enlargeTuffer(bytes.byteLength);
		
		//Add the barr
		this._tuffer.set(bytes, offset);
	}
	
	//========= STRING
	writeString(value) {
		var bytes = (new TextEncoder()).encode(value);
		this.writeUBytes(bytes);
	}
	writeChars(value) {
		var bytes = (new TextEncoder()).encode(value);

		//Prepare its ofset and enlargen
		var offset = this._tuffer.byteLength;
		this._enlargeTuffer(bytes.byteLength);
		
		//Add the barr
		this._tuffer.set(bytes, offset);
	}

	getBuffer() { return this._tuffer; }	
	_addArray(arr) {
		//Prepare the Uint version of the arrayBuffer
		var barr = new Uint8Array(arr.buffer);
		
		//Prepare its ofset and enlargen
		var offset = this._tuffer.byteLength;
		this._enlargeTuffer(barr.byteLength);
		
		//Add the barr
		this._tuffer.set(barr, offset);
	}
	_enlargeTuffer(amount) {	
		var buff = new Uint8Array(this._tuffer.byteLength + amount);
		for (var i = 0; i < this._tuffer.byteLength; i++)  buff[i] = this._tuffer[i];
		
		this._tuffer = buff;
	}
	
}

class ByteStreamReader
{
	
	constructor(arrayBuffer) {
		this._buffer = arrayBuffer;
		this.index = 0;
	}
	
	readByte()
	{ 
		return new Int8Array(this._buffer.slice(this.skip(1), this.index))[0];
	}	
	readUByte()
	{ 
		return new Uint8Array(this._buffer.slice(this.skip(1),  this.index))[0];
	}
	
	readInt() {
		return new Int32Array(this._buffer.slice(this.skip(4),  this.index))[0];
	}	
	readUInt() {
		return new Uint32Array(this._buffer.slice(this.skip(4),  this.index))[0];
	}
		
	readShort() {
		return new Int16Array(this._buffer.slice(this.skip(2),  this.index))[0];
	}		
	readUShort() {
		return new Uint16Array(this._buffer.slice(this.skip(2),  this.index))[0];
	}

	readString() {
		var bytes = this.readUBytes();
		if (bytes == null) return "";
		
		return this._decode(bytes);
	}
	readChars(count) {
		var bytes = new Uint8Array(this._buffer.slice(this.skip(count),  this.index));
		return this._decode(bytes);
	}
	
	readBytes() {
		var length = this.readInt()
		;if (length == 0) return null;
		
		return new Int8Array(this._buffer.slice(this.skip(length),  this.index));
	}
	readUBytes() {
		var length = this.readInt();
		if (length == 0) return null;
		
		return new Uint8Array(this._buffer.slice(this.skip(length),  this.index));
	}
	
	skip(amount) {
		var v = this.index;
		this.index += amount;
		
		return v;
	}
		
	_decode(bytes) { return (new TextDecoder()).decode(bytes); }
}