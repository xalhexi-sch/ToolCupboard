# Tool Cupboard
A custom buildings decay system for Unturned inspired by Rust's Tool Cupboard mechanic.

## Features
- Automatically damages structures and barricades without protection
- Automatically repairs structures and barricades that are protected
- Customizable protection items (claim flags, generators, beds, or custom items)
- Performance-friendly (runs asynchronously and executes safe batched updates)
- Optional UI that warns players when their building is decaying
- Highly configurable to fit your server's needs

## Workshop Item (optional)
- **Tool Cupboard UI** - `3239984557`

---

## Configuration

### Main Settings
- **`DecayUIService`**: Shows a UI warning when players are in a decaying building
- **`DecayService`**: Controls how buildings decay when not protected
- **`HealingService`**: Controls how buildings repair when protected

### Protection Methods
You can enable any of these methods to protect buildings:

1. **Claim Flags (`UseClaimFlags`)**: Uses standard territory claim flags with their default protection radius. Buildings within a player's claimed territory are protected.
2. **Generators (`UseGenerators`)**: Uses powered generators with their default wire range. Set `ShouldGeneratorsRequireFuel` to `true` if you want generators to require fuel to provide protection.
3. **Beds (`UseBeds`)**: Uses player beds with a configurable protection radius (`BedRadius`). Set `ShouldBedsBeClaimed` to `true` if you want only claimed beds to provide protection.
4. **Custom Items (`UseCustomItems`)**: Configure any barricade/structure as a protection device with its own custom radius. Define these in the `CustomItems` section by specifying item IDs and their protection radius:
```xml
<CustomItems>
  <CustomItem Name="Lockers" Radius="50" ItemIds="328 1281" />
</CustomItems>
```

---

### Decay and Healing Settings
- **`Interval`**: How often damage/healing occurs (in seconds)
  - *Example*: `DamageInterval` of `300` means unprotected structures lose health every 5 minutes
  - *Example*: `HealingInterval` of `600` means protected structures heal every 10 minutes
- **`DamagePerInterval` / `HealingPerInterval`**: Amount of health points affected per interval
  - *Example*: With `DamagePerInterval` of `1`, a wooden wall (100 HP) would be destroyed in 100 intervals
  - *Example*: With `HealingPerInterval` of `3`, a damaged wooden wall would fully repair in 34 intervals
- **`UsePercentageDamage` / `UsePercentageHealing`**: Changes how the damage/healing values are calculated
  - When `UsePercentageDamage` is `false` (default): `DamagePerInterval` is a fixed number of HP (e.g., 1 HP per interval)
  - When `UsePercentageDamage` is `true`: `DamagePerInterval` becomes a percentage value (e.g., 5 means 5% of max health)
  - *Example*: For a wooden wall (100 HP), `DamagePerInterval` of `5` with percentage mode ON = 5 HP damage per interval
  - *Example*: For a metal wall (1000 HP), the same settings would cause 50 HP damage per interval

---

### Calculating Decay Times

#### For a Metal Wall (1100 HP):
* **With Fixed Damage (`UsePercentageDamage = false`)**:
  - `DamagePerInterval = 5 HP`
  - `DamageInterval = 300 seconds` (5 minutes)
  - Calculation: $1100\text{ HP} \div 5\text{ HP} = 220\text{ intervals}$
  - Total time: $220\text{ intervals} \times 5\text{ minutes} = 1100\text{ minutes} = 18.3\text{ hours}$
* **With Percentage Damage (`UsePercentageDamage = true`)**:
  - `DamagePerInterval = 2` (2% per interval)
  - `DamageInterval = 300 seconds` (5 minutes)
  - Damage per interval: $2\% \text{ of } 1100 = 22\text{ HP}$
  - Calculation: $1100\text{ HP} \div 22\text{ HP} = 50\text{ intervals}$
  - Total time: $50\text{ intervals} \times 5\text{ minutes} = 250\text{ minutes} = 4.2\text{ hours}$

#### For a Maple Wall (400 HP):
* **With Fixed Damage (`UsePercentageDamage = false`)**:
  - `DamagePerInterval = 5 HP`
  - Calculation: $400\text{ HP} \div 5\text{ HP} = 80\text{ intervals}$
  - Total time: $80\text{ intervals} \times 5\text{ minutes} = 400\text{ minutes} = 6.7\text{ hours}$
* **With Percentage Damage (`UsePercentageDamage = true`)**:
  - `DamagePerInterval = 2` (2% per interval)
  - Calculation: $400\text{ HP} \div 8\text{ HP} = 50\text{ intervals}$
  - Total time: $50\text{ intervals} \times 5\text{ minutes} = 250\text{ minutes} = 4.2\text{ hours}$

---

### Bypass Options
- **`ShouldBypassBed`**: Prevents Black Bedrolls and other beds from decaying
- **`ShouldBypassOilPump`**: Prevents Pump Jacks from decaying
- **`ShouldBypassFarm`**: Prevents Corn Seeds, Carrot Seeds, and other farming items from decaying
- **`ShouldBypassTank`**: Prevents Water Tanks and similar containers from decaying
- **`ShouldBypassCharge`**: Prevents Demolition Charges, Precision Charges, and other explosives from decaying
- **`ShouldBypassTrap`**: Prevents Landmines, Barbed Wire, and other trap items from decaying
- **`ShouldBypassBeacon`**: Prevents Horde Beacons from decaying
- **`ShouldBypassStorage`**: Prevents Lockers, Maple Crates, and other storage containers from decaying
- **`BypassItems`**: List specific item IDs to exclude from decay/healing

---

### Example Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<ToolCupboardConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <!-- UI Warning Settings -->
  <DecayUIService>
    <IsEnabled>false</IsEnabled>
    <EffectId>8301</EffectId>
    <EffectIconUrl>https://i.imgur.com/RKCMPg4.png</EffectIconUrl>
  </DecayUIService>
  
  <!-- Decay Settings -->
  <DecayService>
    <IsEnabled>true</IsEnabled>
    <UseClaimFlags>true</UseClaimFlags>
    <UseGenerators>false</UseGenerators>
    <ShouldGeneratorsRequireFuel>true</ShouldGeneratorsRequireFuel>
    <UseBeds>false</UseBeds>
    <BedRadius>32</BedRadius>
    <ShouldBedsBeClaimed>true</ShouldBedsBeClaimed>
    <UseCustomItems>false</UseCustomItems>
    <CustomItems>
      <CustomItem Name="Lockers" Radius="50" ItemIds="328 1281" />
    </CustomItems>
    <DamageInterval>300</DamageInterval>
    <DamagePerInterval>1</DamagePerInterval>
    <UsePercentageDamage>false</UsePercentageDamage>
    <ShouldBypassBed>true</ShouldBypassBed>
    <ShouldBypassOilPump>true</ShouldBypassOilPump>
    <ShouldBypassFarm>true</ShouldBypassFarm>
    <ShouldBypassTank>true</ShouldBypassTank>
    <ShouldBypassCharge>true</ShouldBypassCharge>
    <ShouldBypassTrap>true</ShouldBypassTrap>
    <ShouldBypassBeacon>true</ShouldBypassBeacon>
    <ShouldBypassStorage>true</ShouldBypassStorage>
    <BypassItems>
      <Item>0</Item>
    </BypassItems>
  </DecayService>
  
  <!-- Healing Settings -->
  <HealingService>
    <IsEnabled>true</IsEnabled>
    <UseClaimFlags>true</UseClaimFlags>
    <UseGenerators>false</UseGenerators>
    <ShouldGeneratorsRequireFuel>true</ShouldGeneratorsRequireFuel>
    <UseBeds>false</UseBeds>
    <BedRadius>32</BedRadius>
    <ShouldBedsBeClaimed>true</ShouldBedsBeClaimed>
    <UseCustomItems>false</UseCustomItems>
    <CustomItems>
      <CustomItem Name="Lockers" Radius="50" ItemIds="328 1281" />
    </CustomItems>
    <HealingInterval>600</HealingInterval>
    <HealingPerInterval>3</HealingPerInterval>
    <UsePercentageHealing>false</UsePercentageHealing>
    <BypassItems>
      <Item>0</Item>
    </BypassItems>
  </HealingService>
</ToolCupboardConfiguration>
```

---

### Translations
```xml
<?xml version="1.0" encoding="utf-8"?>
<Translations xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Translation Id="UI_TEXT" Value="Your base is decaying!" />
</Translations>
```

---

### Credits
- **Tespy** for sponsoring percentage damage and custom items features.
