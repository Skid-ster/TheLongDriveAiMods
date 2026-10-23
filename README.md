# The Long Drive AI Mods

## Car06 1.9 Turbodiesel Conversion

A diesel conversion of the Car06 petrol engine: 850 RPM idle, 4,500 RPM governor, 180 Nm low-end torque, diesel fuel consumption, diesel audio and diesel-matched automatic clutch engagement. Normal starting, oil and cooling remain relevant.

[Download/install instructions, specifications and testing checklist](src/Car06Diesel/README.md)

**Upgrading from the reactor mod? Remove Singularity.dll first and drain petrol before filling with diesel.**

GitHub Actions builds a warning-strict .NET Framework 4.8 DLL on every push. Download **Car06Diesel** from a successful **Build Car06Diesel** run's artifacts. Reference DLLs in `libs/` are compile-time dependencies, not part of the downloadable mod.
