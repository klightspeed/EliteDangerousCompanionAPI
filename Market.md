# Market
https://companion.orerve.net/market

Example JSON:
```json
{
  "id": 128667761,
  "name": "Jaques Station",
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
  "prohibited": {
    "128672304": "NerveAgents",
    ...
  },
  "commodities": [
    {
      "id": 128049202,
      "name": "HydrogenFuel",
      "legality": "",
      "buyPrice": 98,
      "sellPrice": 94,
      "meanPrice": 108,
      "demandBracket": 0,
      "stockBracket": 2,
      "stock": 61743,
      "demand": 1,
      "statusFlags": [],
      "categoryname": "Chemicals",
      "locName": "Hydrogen Fuel"
    },
    ...
  ]
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

## prohibited
Dictionary of `"id": "name"` of commodities prohibited by the station

## commodities
Dictionary of `"id": {}` of commodities traded by the station

### id
Commidity id of the commodity

### name
Symbol name of the commodity

### legality
Empty if legal at station

### buyPrice
Buy price in credits per tonne (i.e. price at which station sells commodity)

### sellPrice
Sell price in credits per tonne (i.e. price at which station buys commodity)

### meanPrice
Average galactic price in credits per tonne

### demandBracket

### stockBracket

### demand
Amount station is willing to buy

### stock
Amount station has available to sell

### statusFlags

### categoryName
Category of commodity

### locName
Localised name of commodity