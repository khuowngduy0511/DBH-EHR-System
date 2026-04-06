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
It refreshes the connection profile key path from the crypto Docker volume,
clears stale Explorer wallet/database volumes, and starts clean.

## Crypto volume

Explorer reads network crypto from a Docker volume, not from a copied local `organizations` folder.

- Volume name is controlled by `FABRIC_CRYPTO_VOLUME` in `.env`.
- Default is `fabric-crypto`.