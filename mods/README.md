# Mods

Each installed mod is packaged in a folder named after the mod. The compiled DLL is kept beside its README, for example:

```text
mods/
└── Lada Restomod/
    ├── M_LadaRestomod.dll
    └── README.md
```

The GitHub Actions build refreshes the DLL in its mod folder whenever the workflow runs on a branch push, and also uploads the packaged folder as a downloadable artifact.
