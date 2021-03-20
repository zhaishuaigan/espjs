var val = false;
setInterval(function () {
	digitalWrite(NodeMCU.D4, val);
	val = !val;
}, 1000);