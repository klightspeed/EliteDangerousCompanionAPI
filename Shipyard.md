# Shipyard
https://companion.orerve.net/shipyard

Example JSON
```json
{
  "id": marketid,
  "name": "stationname",
  "outpostType": "starport",
  "imported": {
    "128682044": "ConductiveFabrics",
    ...
  },
  "exported": {
    "128049202": "HydrogenFuel",
    ...
  },
  "services": {
    "dock": "ok",
    ...
  },
  "economies": {
    "23": {
      "name": "HighTech",
      "proportion": 0.7
    },
    ...
  },
  "ships": {
    "shipyard_list": {
      "Dolphin": {
        "id": 128049291,
        "name": "Dolphin",
        "basevalue": 1337323,
        "sku": "ELITE_HORIZONS_V_PLANETARY_LANDINGS"
      },
      ...
    },
    "unavailable_list": []
  },
  "modules": {
    "128049511": {
      "id": 128049511,
      "category": "weapon",
      "name": "Hpt_AdvancedTorpPylon_Fixed_Large",
      "cost": 157960,
      "sku": "ELITE_HORIZONS_V_PLANETARY_LANDINGS"
    },
    ...
  }
}
```

## imported
Dictionary of `"id": "name"` of commodities imported by the system

## exported
Dictionary of `"id": "name"` of commodities exported by the system

## services
Dictionary of `"name": "status"` of services offered by the station

## economies
Dictionary of `"id": {"name", "proportion"}` of station economies

## ships.shipyardList
Dictionary of `"symbol": {}` of ships sold by the station

### id
Commodity ID of ship

### name
Symbol name of ship

### basevalue
Ship price in credits

### sku
Specifies minimum season / expansion required to purchase

## ships.unavailable_list
Possibly indicates which ships are unavailable for purchase

## modules
Dictionary of `"id": {}` of modules sold by the station

### id
Commodity ID of module

### category
Module category

### name
Symbol name of module

### cost
Module price in credits

### sku
Specifies minimum season / expansion required to purchase