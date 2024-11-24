# MQTT crypto mining controller

A small utility that listens to MQTT protocol and starts up mining.

Winter is coming. I'm putting together old hardware to build a crypto heater for my office. Trying to hook this into
HomeAssistant thermostat.

## Architecture

```asciiflow
    ┌──────────────────────────────┐  
    │  Thermostat                  │  External device or software. Eg. Home assistant generic_thermostat.
    └──────────────────────────────┘  
                |                     
    ┌──────────────────────────────┐  
    │  MQTT Server (eg. mosquitto) │   
    └───▲─────────────────┬────────┘  
        | sub             | pub
    ┌─────────────────────▼────────┐  
    │  MqttService |   Publisher   │  Subscribes to thermostat's switch state topic, wites it to buffer (desired state).
    │              |    Service    │  Publish Command states to status topic (the actual state at the moment).
    │------------------------------│
    │  Input       │  Output       │  Hold desired state and the output state in separate buffers.
    │   Buffer     │   Buffer      │  
    │------------------------------│
    │  CommandCollection           │  Coordinates execution of multiple Commands. Reads desired state from input buffer  
    │                              │  and publishes status to output buffer.
    │------------------------------│
    │  Command                     │  Individual processes. Support for 2 commands.
    └──────────────────────────────┘  
        | start           | kill
    ┌───▼─────────────────▼────────┐  
    │  Process (OS)                │  
    │  (Miner softwares)           │ 
    │                              │ 
    └──────────────────────────────┘ 
```

## Topic config

* `/kolikko1/heat/status` KolikkoControl will publish it's state here, `ON` or `OFF`
* `/kolikko1/heat` KolikkoControl receives `ON` or `OFF` commands.
* `/kolikko1/heat/statusmsg` KolikkoControl might send some status info here
* `/kolikko1/heat/command/hashrate` KolikkoControl will post hash rate in Kh/s (not implemented, needs better design)
* `/kolikko1/heat/command/accepted` KolikkoControl will post accepted share amount here (not implemented, ...) 


