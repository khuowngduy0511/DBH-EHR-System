# Explorer Helper

Use the Bash helper script in this folder instead of typing long Docker commands.

## Commands

```bash
# Normal start/restart (keeps wallet/db volumes)
./explorer.sh start

# Full reset after recreating Fabric network/crypto
./explorer.sh reset

# Check container status
./explorer.sh status

# Tail explorer logs
./explorer.sh logs
```

## When to use reset

Run `reset` after regenerating organizations/MSP crypto or rebuilding the network.
It refreshes copied crypto, clears stale Explorer wallet/database volumes, and starts clean.