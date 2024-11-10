# MQTT crypto mining controller

A small utility that listens to MQTT protocol and starts up mining.

Winter is coming. I'm putting together old hardware to build a crypto heater for my office. Trying to hook this into
HomeAssistant thermostat.

## Topic config

* `/kolikko1/heat/status` KolikkoControl will publish it's state here, `ON` or `OFF`
* `/kolikko1/heat` KolikkoControl receives `ON` or `OFF` commands.
* `/kolikko1/heat/statusmsg` KolikkoControl might send some status info here
* `/kolikko1/heat/command/hashrate` KolikkoControl will post hash rate in Kh/s (not implemented)
* `/kolikko1/heat/command/accepted` KolikkoControl will post accepted share amount here (not implemented) 
