---
title: "Home"
description: "The home page for the Grid Bot!"
---

# Announcement

As of Tuesday, the 21st of March 2024, Grid Bot's infrastructure was turned off.

The reasoning behind this was simply put, it became majorily costly.

This bot ran on this stack of infrastructure on AWS:

1 Linux machine, @ 16 cores, 32 GiB -- This hosted the Bot, as well as its underlying dependencies such as Redis, Consul and grid-service-websrv
1 Client VPN -- This prevented outside access to internal components such as configuration and a job management.

Overall, this costed upwards of $1000 a month to maintain fully, which did get cost draws but in the end, for the whole year it would nearly cost $11.000.

## What now?

The position of hosting this is open for applications, if people wish to work with me they may open an issue on [GitHub](https://github.com/mfdlabs/grid-bot) or email me [petko@vmminfra.net](mailto:petko@vmminfra.net). You must have infrastructure capabilities to handle the following:

- Constant throughput from ~31.000 guilds every second.
- Enough storage and compute power to handle clients hosting upwards of 30 shards.
