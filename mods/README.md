# Mods

Each mod has a folder named after its in-game title, with the compiled DLL and a short README inside:

```text
mods/
└── Car 04 Race Restomod/
    ├── M_Car04RaceRestomod.dll
    └── README.md
```

GitHub Actions refreshes the compiled DLL in its mod folder on successful branch-push builds, and also uploads the packaged folder as a downloadable artifact.
