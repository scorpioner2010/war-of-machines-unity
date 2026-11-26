# RAV deployment on AlienX Cloud Lite

## Current gate

Cloud access is invitation-only. The account's single free access request is ticket **#93**, currently waiting for staff. No VM, payment method, paid feature, or cloud resource has been created. Do not retry the access request or create a VM until staff approves the account and confirms the free Lite allocation.

## Fit and limitations

The authenticated panel showed the free Lite allowance as 1 vCPU, 2 GB RAM, 30 GB disk, 2 projects, 10 WAN ports, and 10 Mbit/s. Ubuntu 24.04 LTS appeared in the OS list; the VM architecture and actual allocation remain unverified until a VM exists. The observed 10 Mbit/s is lower than the 50 Mbps estimate in the task brief.

AlienX Cloud supports inbound UDP mappings and lets the owner choose UDP separately. It uses a shared public IPv4 and assigned port ranges; the public game port and VM listening port must be the same number. It does not translate an arbitrary public port to internal UDP 7770. The provider may change assigned ports, and no stable public IP/endpoint guarantee was found. SSH also consumes a WAN port. Configure both the AlienX panel firewall and UFW for the panel-assigned SSH port and the assigned UDP game port.

RAV already accepts RAV_SERVER_PORT at startup and uses that value for both FishNet's UDP bind and API registration. Set it to the actual assigned AlienX UDP port; do not assume 7770 is available. Set RAV_SERVER_ADDRESS to the current public shared address. DTLS stays enabled and its certificate/key remain systemd credentials. No gameplay/networking code change is currently needed for the documented same-port mapping.

The published terms list Russia among blocked IP locations and do not list Ukraine. Do not use VPNs, proxies, or tunnels to bypass restrictions. AlienX describes Cloud as a hobby beta on shared residential hardware, without an uptime/SLA promise; do not use it as production or promise 24/7 availability. The 1-vCPU/2-GB Lite allocation is only a candidate for low-scale smoke tests. A 20-player match is unverified.

## Deployment method

Use the existing native Linux/systemd deployment documented in [the Linux deployment README](../linux/README.md). No Dockerfile or duplicate host scripts are needed. Keep PostgreSQL private and do not deploy the API/database to Lite without a separate resource assessment. RAV_API_BASE must point to an isolated HTTPS staging API; no active staging endpoint was available during this assessment, so do not use production credentials or production services.

Current source HEAD: 3a050ee43598d5ff6aea16f17cd243e677043534.
Current limited-test Linux build: D:\Programing\UnityProjects\war-of-machines-local-builds\LinuxServer-20261124-192541.
Release archive: D:\Programing\UnityProjects\war-of-machines-local-builds\AlienX-RAV-Lite-Release-20261125\RAVServer-AlienX-Lite-20261125.tar.gz.
SHA-256: 80216b46ac3e520bd6100543e9517ce26bba9dc81e4f88eff9414dc20c9bd030.

The build contains Server and Map scenes, UnityPlayer.so, Assembly-CSharp.dll, and the Mono runtime. It is a Unity Development build for limited staging validation. The archive excludes RAV_BurstDebugInformation_DoNotShip, contains no detected secret files or symlinks, has safe relative paths and a 0755 server executable, and passed archive/member checks. This is a local artifact check, not Linux runtime validation.

## Required steps after access approval

1. Confirm the VM creation page still shows the Lite tier at zero cost, no paid boost, and no unexpected port charge; choose Ubuntu 24.04 and record the actual architecture/resources.
2. Record the assigned public address, SSH port, and UDP port. Create only the UDP mapping that is included in the free allocation.
3. Confirm SSH host fingerprint before connecting. Bootstrap with the existing Linux scripts; keep a verified alternate console/session while configuring UFW.
4. Set RAV_SERVER_PORT to the assigned UDP port and RAV_SERVER_ADDRESS to the current public address. Keep DTLS enabled and provide certificates through the existing systemd credential mechanism.
5. Use only an isolated HTTPS staging API and staging credentials. If none is available, stop before server registration; do not point the server at production.
6. Verify service startup, Server/Map runtime loading, UDP listener, external UDP reachability, DTLS handshake, API registration/heartbeat/ticket flow, and graceful shutdown. A local listener or SSH success does not prove external UDP or DTLS.
7. Measure CPU, resident memory, packet rate and bandwidth with small tests before increasing player count. Do not claim 20 slots until a real load test passes.

## References

- [AlienX Cloud networking](https://www.alienxservers.co.uk/knowledgebase/cloud/how-cloud-networking-works)
- [Opening a port on a VM](https://www.alienxservers.co.uk/knowledgebase/cloud/open-a-port)
- [AlienX Terms of Service](https://www.alienxservers.co.uk/terms)

## Локальна перевірка та актуальний артефакт — 2026-11-25

Цей запис оновлює попередній стан артефакту: попередні нотатки про відсутність runtime-перевірки датовані раніше за цей локальний прогін.

- З Unity 6000.3.13f1 зібрано новий Linux x86-64 Dedicated Server Development build через наявний RAV.Editor.LocalNetworkBuild.BuildLinuxDedicatedServer workflow. Build Result: Success; Unity batchmode завершився з кодом 0. Build log: C:\Users\TorasPC\AppData\Local\Temp\rav-local-053ecf34\unity-linux-build.log.
- Build: D:\Programing\UnityProjects\war-of-machines-local-builds\LinuxServer-20261124-203532.
- Package: D:\Programing\UnityProjects\war-of-machines-local-builds\AlienX-RAV-Lite-Release-20261125-v2\RAVServer-AlienX-Lite-20261125-v2.tar.gz.
- SHA-256: 939ac7e6ef5134605f677760e3ad309e05ea7414d18ab546b0c4190efb95abd6; size 110,793,355 bytes. Archive has 333 entries, executable mode 0755, required Unity/Mono runtime files and relative paths; no detected secret, key, environment, credential, or Burst debug entries.
- The build ran in an isolated Ubuntu 24.04 Docker runtime. Unity headless startup, FishNet UDP listener on 7770 inside the container, Server scene runtime, API registration/heartbeat, and graceful shutdown/unregister passed. Host mapping was loopback-only at 127.0.0.1:7770/udp.
- Local two-player API smoke passed: HTTPS certificate validation; health/live and health/ready; player registration/login/profile and starter vehicle; connection ticket issue/redemption and replay rejection; two-player backend match record.
- API and PostgreSQL containers were healthy; PostgreSQL TLS was verified with verify-full. The Compose API, PostgreSQL, and backend network exposed no host ports. Test resources use an isolated project, network, volumes, and temporary credentials outside Git.

This is a local container rehearsal, not an AlienX deployment. Actual Ubuntu systemd installation, Map scene runtime, real Unity client connection, external UDP reachability, FishNet DTLS handshake, vehicle/bot gameplay, match result/profile update, reconnect, backup/restore, and 20-player performance remain unverified. AlienX ticket #93 still gates VM access. Altare resources are not provisioned. Do not use production services or claim end-to-end cloud multiplayer success.
