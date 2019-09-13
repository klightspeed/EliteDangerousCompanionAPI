# Profile
https://companion.orerve.net/profile

Example JSON:
```json
{
  "commander": {
    "id": xxxxx,
    "name": "Traumatophobiac",
    "credits": xxxxx,
    "debt": 0,
    "currentShipId": 4,
    "alive": true,
    "docked": true,
    "rank": {
      "combat": 0,
      "trade": 3,
      "explore": 6,
      "crime": 0,
      "service": 0,
      "empire": 0,
      "federation": 0,
      "power": 0,
      "cqc": 0
    }
  },
  "lastSystem": {
    "id": 3238296097059,
    "name": "Colonia",
    "faction": "independent"
  },
  "lastStarport": {
    "id": 128667761,
    "services": {
      "dock": "ok",
      ...
    },
    "name": "Jaques Station",
    "faction": "independent",
    "minorfaction": "Jaques"
  },
  "ship": {
    "id": 4,
    "name": "CobraMkIII",
    "value": {
      "hull": 174498,
      "modules": 1962114,
      "cargo": 17326,
      "total": 2153938,
      "unloaned": 32299
    },
    "free": false,
    "shipName": "Peaceful Pancake",
    "shipID": "EE-224",
    "station": {
      "id": 128667761,
      "name": "Jaques Station"
    },
    "starsystem": {
      "id": 3238296097059,
      "name": "Colonia",
      "systemaddress": 3238296097059
    },
    "alive": true,
    "health": {
      "hull": 1000000,
      "shield": 1000000,
      "shieldup": true,
      "integrity": 499393,
      "paintwork": 1000000
    },
    "cockpitBreached": false,
    "oxygenRemaining": 450000,
    "modules": {
      "TinyHardpoint1": {
        "module": {
          "id": 128049513,
          "name": "Hpt_ChaffLauncher_Tiny",
          "locName": "Chaff",
          "locDescription": "Signature tracking defence. When deployed, causes gimbal and turret-mounted devices to lose lock. Requires ammunition.",
          "value": 8500,
          "free": false,
          "health": 925000,
          "on": false,
          "priority": 0
        }
      },
      ...,
      "PowerPlant": {
        "module": {
          "id": 128064034,
          "name": "Int_Powerplant_Size2_Class2",
          "locName": "Power Plant",
          "locDescription": "Consumes fuel to power all ship modules.",
          "value": 5934,
          "free": false,
          "health": 1000000,
          "on": true,
          "priority": 1
        },
        "engineer": {
          "engineerName": "Felicity Farseer",
          "engineerId": 300100,
          "recipeName": "PowerPlant_Boosted",
          "recipeLocName": "Overcharged Power Plant",
          "recipeLocDescription": "This enhancement loses module integrity and increases thermal load to provide increased power generation.",
          "recipeLevel": 1
        },
        "WorkInProgress_modifications": {
          "OutfittingFieldType_PowerCapacity": {
            "value": 1.1939474982513769,
            "LessIsGood": false,
            "locName": "Power capacity",
            "displayValue": "19.39%",
            "dir": "˄"
          },
          "OutfittingFieldType_Integrity": {
            "value": 0.938452828675508,
            "LessIsGood": false,
            "locName": "Integrity",
            "displayValue": "-6.15%",
            "dir": "˅"
          },
          "OutfittingFieldType_HeatEfficiency": {
            "value": 1.0790880247950549,
            "LessIsGood": true,
            "locName": "Heat efficiency",
            "displayValue": "-7.91%",
            "dir": "˅"
          }
        },
        "specialModifications": []
      },
      ...
    },
    "launchBays": {
      "Slot04_Size2": {
        "SubSlot0": {
          "name": "testbuggy",
          "locName": "SRV Scarab",
          "rebuilds": 1,
          "loadout": "starter",
          "loadoutName": "Starter"
        }
      }
    }
  },
  "ships": {
    "4": {
      "id": 4,
      "name": "CobraMkIII",
      "value": {
        "hull": 174498,
        "modules": 1962114,
        "cargo": 17326,
        "total": 2153938,
        "unloaned": 32299
      },
      "free": false,
      "shipName": "Peaceful Pancake",
      "shipID": "EE-224",
      "station": {
        "id": 128667761,
        "name": "Jaques Station"
      },
      "starsystem": {
        "id": 3238296097059,
        "name": "Colonia",
        "systemaddress": 3238296097059
      }
    },
    ...
  }
}
```

## commander
Commander details

### id
Commander ID

### name
Commander Name

### credits
Credit balance

### debt
Credits loaned to commander

### currentShipId
ID of current ship

### alive
False if commander has died and not yet resurrected

### docked
True if commander is docked at a station

### rank
Commander numeric rank for each major power and game dimension (0 = lowest - e.g. Harmless)

## lastSystem
Details of system commander was last in

### id
System index (for manually placed systems) or system address of system.

Note that almost all catalogue systems (e.g. HIP, HD, Gliese) and many
systems that have been named since the start are manually placed and
the id represents the index in the system table for these systems.

Sol has system index 0

The highest system index known is 145197 ([V616 Monocerotis](https://www.edsm.net/en/system/id/4012327/name/V616+Monocerotis)), while the
lowest known visited procgen system address is 752903 ([Eephonth AA-A h0](https://www.edsm.net/en/system/id/19119213/name/Eephonth+AA-A+h0)).

### name
System name

### faction
System controlling minor faction

## lastStarport
Details of starport commander was last docked at

### id
Market ID of station

### services
Dictionary of `"name": "status"` of services offered by the station
Empty if not currently docked

### name
Station Name
Empty if not currently docked

### faction
Station allegiance
Empty if not currently docked

### minorfaction
Minor faction controlling station
Empty if not currently docked

## ship
Details of current ship

### id
Persistent index of ship

### name
Ship type

### value
Dictionary of `"part": value` of ship value

### shipName
Commander-assigned ship name

### shipID
Commander-assigned ship ID

### station
Details of station ship was last docked at

### system
Details of system ship was last docked in

#### id
System index (for manually placed systems) or system address (id64) of system.

#### name
System name

#### systemaddress
System Address (id64)

### alive
False if ship was destroyed but not yet rebought

### health
Health details of ship

#### hull
Hull integrity, where 1000000 is 100%

#### shield
Shield integrity, where 1000000 is 100%

#### shieldup
True if shield is up

#### integrity
Structural integrity, where 1000000 is 100%

#### paintwork
Paint integrity, where 1000000 is 100%

### cockpitBreached
True if the canopy has been breached

### oxygenRemaining
Remaining oxygen in milliseconds

### modules
Dictionary of `"slot": {}` of module details

#### module.id
Commodity ID of module

#### module.name
Symbol name of the module

#### module.locName
Localised name of the module

#### module.locDescription
Localised description of the module

#### module.value
Purchase price of module in credits

#### module.free
True if module came with a free ship

#### module.health
Module health, where 1000000 is 100%

#### module.on
False if module is turned off

#### module.priority
Power priority set on module

#### engineer.engineerName
Name of engineer that modified the module

#### engineer.engineerId
ID of the engineer that modified the module

#### engineer.recipeName
Symbol name of recipe used to modify the module

#### engineer.recipeLocLane
Localised name of the recipe used to modify the module

#### engineer.recipeLocDescription
Localised description of recipe

#### engineer.recipeLevel
Level of recipe used to modify the module

## ships
Dictionary of `"id": {}` of Details of all ships owned by commander

