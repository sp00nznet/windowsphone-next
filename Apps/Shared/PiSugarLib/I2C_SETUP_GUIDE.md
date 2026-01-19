# PiSugar2 Plus I2C Setup Guide for LattePanda 3 Delta

## Overview

This guide explains how to physically connect the PiSugar2 Plus battery to the LattePanda 3 Delta via I2C for battery monitoring. Unlike Raspberry Pi (which uses pogo pins), the LattePanda requires a manual I2C connection.

---

## Why I2C Instead of TCP Socket?

### The Problem
The PiSugar2 TCP socket API (`pisugar-server` on port 8423) is **Raspberry Pi specific**:
- Requires pogo pin GPIO connection
- Runs Linux-based daemon software
- Not available on x86 Windows (LattePanda)

### The Solution
**Direct I2C communication** with the IP5209/IP5312 power management IC:
- Hardware-level access to battery data
- Works on any platform with I2C support
- More reliable and lower latency

---

## Hardware Requirements

### Components Needed

| Item | Specification | Qty | Est. Cost | Where to Buy |
|------|---------------|-----|-----------|--------------|
| **Jumper Wires** | 26AWG Female-Female, 20cm | 3 | $2-5 | Amazon, AliExpress |
| **Pull-up Resistors** | 4.7kΩ, 1/4W, 5% tolerance | 2 | $1 | DigiKey, Mouser |
| **Solder Wire** | Lead-free, 0.8mm, rosin core | - | $5-10 | Amazon, local electronics |
| **Soldering Iron** | Temperature controlled (300-350°C) | 1 | $15-50 | Amazon, local electronics |
| **Heat Shrink Tubing** | 2mm diameter, black | 6pc | $3 | Amazon, eBay |
| **Multimeter** | Digital, continuity + voltage | 1 | $10-30 | Amazon, Harbor Freight |

**Optional Tools:**
- Desoldering pump or wick (for mistakes)
- Helping hands/PCB holder
- Logic analyzer (for I2C debugging)
- Magnifying glass or microscope

**Total Cost**: ~$20-50 (excluding optional tools)

---

## PiSugar2 Plus Pinout

### Locating I2C Pins

The PiSugar2 Plus has I2C test pads on the PCB. Location varies by version:

**Method 1: Test Pads (Most Common)**
```
┌─────────────────────────────┐
│ PiSugar2 Plus PCB (Bottom)  │
│                             │
│  ┌─────────────┐           │
│  │ Battery     │           │
│  │ Connector   │  [USB-C]  │
│  └─────────────┘           │
│                             │
│  Test Pads (Near Edge):    │
│  ○ SDA  ← I2C Data         │
│  ○ SCL  ← I2C Clock        │
│  ○ GND  ← Ground           │
│  ○ 3.3V ← Power (optional) │
│                             │
└─────────────────────────────┘
```

**Method 2: Pogo Pin Header (If Exposed)**
Some models expose the I2C pins on the pogo pin connector intended for Raspberry Pi.

### IP5209/IP5312 Specifications

- **I2C Address**: `0x75` (or `0x32` for some models)
- **Clock Speed**: 100kHz (Standard Mode) or 400kHz (Fast Mode)
- **Logic Level**: 3.3V
- **Pull-up Resistors**: Required (4.7kΩ to 3.3V)

---

## LattePanda 3 Delta Pinout

### Arduino Leonardo Compatible Headers

The LattePanda 3 Delta has Arduino Leonardo compatible GPIO headers with I2C support:

```
┌─────────────────────────────────────────────────────┐
│ LattePanda 3 Delta - Arduino Leonardo Header       │
│                                                     │
│  Digital Pins:                                      │
│  ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐                    │
│  │0│ │1│ │2│ │3│ │4│ │5│ │6│ ... (continues)      │
│  └─┘ └─┘ └─┘ └─┘ └─┘ └─┘ └─┘                    │
│         ↑   ↑                                       │
│        SDA SCL                                      │
│       (D2) (D3)                                     │
│                                                     │
│  Pin 20 (D2): SDA - I2C Data                       │
│  Pin 21 (D3): SCL - I2C Clock                      │
│  GND: Any ground pin                                │
│  3.3V: For pull-up resistors                       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Important**: The LattePanda uses **3.3V logic** for I2C, matching the PiSugar2.

---

## Wiring Diagram

### Complete Connection Schematic

```
PiSugar2 Plus                LattePanda 3 Delta
┌──────────────┐            ┌─────────────────────────┐
│              │            │   Arduino Headers        │
│  Test Pads:  │            │                          │
│              │            │                          │
│   ○ SDA ─────┼────────────┼─> Pin 20 (D2/SDA)       │
│     │        │  Blue      │      │                   │
│     │        │            │      │                   │
│   ○ SCL ─────┼────────────┼─> Pin 21 (D3/SCL)       │
│     │        │  Yellow    │      │                   │
│     │        │            │      │                   │
│   ○ GND ─────┼────────────┼─> GND                   │
│              │  Black     │                          │
│              │            │   Pull-up Resistors:     │
│              │            │   SDA ──┤4.7kΩ├── 3.3V  │
│              │            │   SCL ──┤4.7kΩ├── 3.3V  │
│              │            │                          │
└──────────────┘            └─────────────────────────┘

Wire Color Legend:
- Blue:   SDA (Data)
- Yellow: SCL (Clock)
- Black:  GND (Ground)
```

### Pull-up Resistor Configuration

I2C requires pull-up resistors on both SDA and SCL lines:

```
        3.3V
         │
         ├───[4.7kΩ]─── SDA
         │
         └───[4.7kΩ]─── SCL

                    │
                   GND
```

**Note**: Check if LattePanda already has built-in pull-ups using a multimeter. If SDA/SCL measure ~3.3V with no devices connected, pull-ups are present.

---

## Step-by-Step Assembly

### Step 1: Prepare PiSugar2 Plus

1. **Open the Case** (if applicable):
   - Carefully remove screws
   - Lift top cover
   - Expose PCB

2. **Locate I2C Test Pads**:
   - Look for labels: `SDA`, `SCL`, `GND`
   - Usually near battery connector or edge of PCB
   - May be on bottom side of PCB

3. **Clean Test Pads**:
   - Use isopropyl alcohol and cotton swab
   - Remove any oxidation or coating
   - Let dry completely

### Step 2: Solder Wires to PiSugar2

**Safety**: Work in ventilated area, use proper soldering technique.

1. **Tin the Test Pads**:
   ```
   - Heat soldering iron to 350°C
   - Touch pad with tip for 1 second
   - Apply small amount of solder
   - Remove tip quickly (don't overheat!)
   ```

2. **Prepare Wires**:
   ```
   Wire Lengths:
   - SDA: 15-20cm (Blue wire)
   - SCL: 15-20cm (Yellow wire)
   - GND: 15-20cm (Black wire)

   Preparation:
   - Strip 2mm insulation from one end
   - Tin the stripped end with solder
   ```

3. **Solder Wires to Pads**:
   ```
   For each wire:
   1. Position tinned wire on tinned pad
   2. Touch with soldering iron (1-2 seconds)
   3. Let cool without moving
   4. Gently pull to test connection
   5. Slide heat shrink over joint
   6. Heat with heat gun or lighter side
   ```

4. **Verify Connections**:
   ```
   Using multimeter (continuity mode):
   - Wire end to test pad: BEEP ✓
   - Between different pads: NO BEEP ✓
   ```

### Step 3: Connect to LattePanda

1. **Power Off LattePanda** completely

2. **Locate Arduino Header Pins**:
   - Pin 20 (D2/SDA)
   - Pin 21 (D3/SCL)
   - GND (any ground pin)

3. **Connect Wires**:
   ```
   PiSugar2        LattePanda
   SDA (Blue)   →  Pin 20 (D2)
   SCL (Yellow) →  Pin 21 (D3)
   GND (Black)  →  GND
   ```

4. **Add Pull-up Resistors** (if needed):

   **Method A: Breadboard**
   ```
   1. Place mini breadboard near LattePanda
   2. Insert 4.7kΩ resistor in breadboard
   3. Connect one leg to SDA, other to 3.3V
   4. Repeat for SCL
   ```

   **Method B: Direct Soldering** (advanced)
   ```
   1. Solder one leg of resistor to SDA wire
   2. Solder other leg to 3.3V pin
   3. Repeat for SCL
   4. Insulate with heat shrink
   ```

5. **Secure Wires**:
   - Use zip ties or adhesive mounts
   - Prevent strain on solder joints
   - Ensure no shorts between wires

### Step 4: Testing & Verification

1. **Visual Inspection**:
   ```
   Check:
   ✓ No exposed wire touching other connections
   ✓ Heat shrink covering all solder joints
   ✓ Wires not under tension
   ✓ Correct pins connected
   ```

2. **Multimeter Tests** (Power OFF):
   ```
   Continuity Mode:
   - PiSugar SDA → LattePanda Pin 20: BEEP ✓
   - PiSugar SCL → LattePanda Pin 21: BEEP ✓
   - PiSugar GND → LattePanda GND: BEEP ✓
   - SDA to SCL: NO BEEP ✓
   - SDA/SCL to GND: NO BEEP ✓

   Resistance Mode:
   - SDA to 3.3V: ~4.7kΩ (pull-up present) ✓
   - SCL to 3.3V: ~4.7kΩ (pull-up present) ✓
   ```

3. **Power On Test**:
   ```
   1. Connect PiSugar2 to power (USB-C)
   2. Power on LattePanda
   3. Check voltage with multimeter:
      - SDA: ~3.3V when idle ✓
      - SCL: ~3.3V when idle ✓
   ```

4. **Software Detection** (Windows):
   ```powershell
   # Check I2C devices (requires admin PowerShell)
   Get-PnpDevice | Where-Object {$_.FriendlyName -like "*I2C*"}

   # Should show I2C controller device
   ```

---

## Software Configuration

### Enable I2C in Windows

I2C on LattePanda requires enabling the Arduino interface:

1. **Install LattePanda Drivers**:
   - Download from: https://github.com/LattePandaTeam/LattePanda-Win10-Software
   - Install Arduino Leonardo drivers

2. **Enable I2C in BIOS** (if needed):
   ```
   1. Restart LattePanda
   2. Press DEL/F2 to enter BIOS
   3. Navigate to: Advanced → Onboard Devices
   4. Enable: "Arduino Co-processor"
   5. Save and exit
   ```

3. **Verify Device Manager**:
   ```
   1. Open Device Manager
   2. Expand: "System devices"
   3. Look for: "Arduino Leonardo" or "I2C Controller"
   ```

### Test I2C Communication

Use the PiSugarI2CController library:

```csharp
using WindowsPhoneNext.PiSugarLib;

var battery = new PiSugarI2CController();

if (await battery.InitializeAsync())
{
    Console.WriteLine("PiSugar2 detected on I2C!");

    var level = await battery.GetBatteryLevelAsync();
    var voltage = await battery.GetBatteryVoltageAsync();

    Console.WriteLine($"Battery: {level}%");
    Console.WriteLine($"Voltage: {voltage:F2}V");
}
else
{
    Console.WriteLine("Failed to detect PiSugar2");
    Console.WriteLine("Check wiring and I2C address");
}
```

---

## Troubleshooting

### Problem: I2C Device Not Detected

**Symptoms**: `InitializeAsync()` returns false

**Solutions**:
1. ✓ Verify physical connections with multimeter
2. ✓ Check pull-up resistors are present (SDA/SCL = 3.3V idle)
3. ✓ Try alternative I2C address (0x32 instead of 0x75)
4. ✓ Ensure Arduino Leonardo drivers installed
5. ✓ Check Device Manager for I2C controller
6. ✓ Verify BIOS has Arduino co-processor enabled

**Testing Different I2C Address**:
```csharp
// Try 0x32 if 0x75 doesn't work
// Modify in PiSugarI2CController.cs:
private const byte I2C_ADDRESS = 0x32; // Was 0x75
```

### Problem: Incorrect Battery Readings

**Symptoms**: Battery shows 0%, negative voltage, or nonsensical values

**Solutions**:
1. ✓ Verify correct register addresses for your IC model (IP5209 vs IP5312)
2. ✓ Check conversion formulas match your hardware revision
3. ✓ Ensure stable power supply to PiSugar2
4. ✓ Test with known-good battery charge level

### Problem: Intermittent Connection

**Symptoms**: Readings work sometimes, fail other times

**Solutions**:
1. ✓ Check for loose solder joints - re-solder if needed
2. ✓ Verify wires aren't under mechanical stress
3. ✓ Add strain relief to wire connections
4. ✓ Check for electromagnetic interference (move away from motors/PSUs)
5. ✓ Verify pull-up resistors are properly connected

### Problem: Short Circuit / No Power

**Symptoms**: LattePanda won't boot, smoke, or burning smell

**IMMEDIATE ACTION**:
1. ⚠️ DISCONNECT POWER IMMEDIATELY
2. ⚠️ Check for shorts with multimeter (power OFF)
3. ⚠️ Verify no solder bridges between pins
4. ⚠️ Check polarity of all connections

### Problem: Windows Can't Access I2C

**Symptoms**: Software fails with access denied or device not found

**Solutions**:
1. ✓ Run application as Administrator
2. ✓ Check app manifest has device capabilities:
   ```xml
   <DeviceCapability Name="lowLevelDevices"/>
   ```
3. ✓ Verify Windows.Devices.I2c package reference in project

---

## Register Map Reference

### IP5209/IP5312 I2C Registers

| Register | Address | Bits | Description | Read/Write |
|----------|---------|------|-------------|------------|
| SYS_CTL0 | 0x00 | 8 | System control 0 | R/W |
| SYS_CTL1 | 0x01 | 8 | System control 1 | R/W |
| SYS_CTL2 | 0x02 | 8 | System control 2 | R/W |
| READ0 | 0xA0 | 8 | Battery voltage [15:8] | R |
| READ1 | 0xA1 | 8 | Battery voltage [7:0] | R |
| READ2 | 0xA2 | 8 | Battery current [15:8] | R |
| READ3 | 0xA3 | 8 | Battery current [7:0] | R |
| READ4 | 0xA4 | 8 | Battery percentage | R |
| CHG_DIG_CTL0 | 0x22 | 8 | Charge digital control | R/W |

### Data Conversion Formulas

**Battery Voltage** (16-bit):
```
Raw Value = (READ0 << 8) | READ1
Voltage (mV) = (Raw Value × 0.26855) + 2600
Voltage (V) = Voltage_mV ÷ 1000
```

**Battery Current** (16-bit signed):
```
Raw Value = (READ2 << 8) | READ3
If Raw Value > 32767:
    Raw Value = Raw Value - 65536  // Convert to signed
Current (mA) = Raw Value × 0.745985
Current (A) = Current_mA ÷ 1000

Positive = Charging
Negative = Discharging
```

**Battery Percentage**:
```
Percentage = READ4 % 101
Valid Range: 0-100
```

---

## Safety Warnings

⚠️ **IMPORTANT SAFETY INFORMATION**:

1. **Soldering Safety**:
   - Work in ventilated area
   - Don't breathe solder fumes
   - Keep soldering iron away from flammable materials
   - Unplug iron when not in use

2. **Electrical Safety**:
   - Always power off before connecting/disconnecting
   - Never work on live circuits
   - Verify connections before applying power
   - Use ESD protection when handling electronics

3. **Battery Safety**:
   - LiPo batteries can be dangerous if damaged
   - Don't short circuit battery terminals
   - Don't puncture or damage battery
   - Don't overheat battery (>60°C)
   - Dispose of damaged batteries properly

4. **Short Circuit Prevention**:
   - Double-check all connections
   - Use multimeter to verify no shorts
   - Insulate all exposed connections
   - Keep metal objects away from live circuits

---

## Alternative Methods

### Option 1: USB-to-I2C Adapter

If you don't want to make direct connections:

**Hardware**:
- CH341A USB-to-I2C adapter (~$5)
- Connect PiSugar2 I2C to adapter
- Plug adapter into USB port

**Pros**: No soldering to LattePanda, removable
**Cons**: Extra hardware, USB port occupied

### Option 2: Arduino Bridge

Use LattePanda's built-in Arduino Leonardo as I2C bridge:

**Method**:
1. Upload I2C sketch to Arduino Leonardo
2. Connect PiSugar2 to Arduino I2C pins
3. Communicate with Arduino via Serial

**Pros**: Uses existing hardware, isolated from main system
**Cons**: More complex software, serial overhead

---

## References

- **PiSugar2 Wiki**: https://github.com/PiSugar/PiSugar/wiki/PiSugar2-Plus
- **IP5209 Datasheet**: https://github.com/PiSugar/PiSugar/tree/master/hardware
- **LattePanda Docs**: https://docs.lattepanda.com/
- **Windows I2C API**: https://docs.microsoft.com/en-us/uwp/api/windows.devices.i2c
- **PiSugar2 Python Client**: https://github.com/PiSugar/pisugar-power-manager-rs (for reference)

---

## Questions?

If you encounter issues not covered in this guide:

1. Check the troubleshooting section above
2. Verify all connections with multimeter
3. Post issue on GitHub with:
   - Photos of your wiring
   - Multimeter readings
   - Error messages
   - Hardware versions

**Good luck with your build!** 🔋⚡
