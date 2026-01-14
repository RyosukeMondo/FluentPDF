#!/bin/bash
# FluentPDF Vagrant Test Runner
# Syncs code and runs build/tests in Windows VM

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

print_header() {
    echo -e "${CYAN}======================================${NC}"
    echo -e "${CYAN}  FluentPDF Vagrant Test Runner${NC}"
    echo -e "${CYAN}======================================${NC}"
    echo
}

print_usage() {
    echo "Usage: $0 [command]"
    echo
    echo "Commands:"
    echo "  up        Start the VM (first run provisions everything)"
    echo "  sync      Sync code from host to VM"
    echo "  build     Build FluentPDF in VM"
    echo "  test      Run unit tests in VM"
    echo "  smoke     Run smoke test in VM"
    echo "  e2e       Run E2E tests in VM (requires RDP session)"
    echo "  all       Sync + Build + Test + Smoke"
    echo "  ssh       SSH into VM"
    echo "  rdp       Open RDP connection to VM"
    echo "  halt      Stop the VM"
    echo "  destroy   Destroy the VM"
    echo
}

check_vagrant() {
    if ! command -v vagrant &> /dev/null; then
        echo -e "${RED}Error: Vagrant not installed${NC}"
        echo "Install with: sudo apt-get install vagrant"
        exit 1
    fi
}

ensure_vm_running() {
    if ! vagrant status | grep -q "running"; then
        echo -e "${YELLOW}VM not running, starting...${NC}"
        vagrant up
    fi
}

cmd_up() {
    echo -e "${CYAN}Starting VM...${NC}"
    vagrant up
    echo -e "${GREEN}VM started. RDP available at localhost:23389 (vagrant/vagrant)${NC}"
}

cmd_sync() {
    ensure_vm_running
    echo -e "${CYAN}Syncing code to VM...${NC}"
    vagrant rsync
    echo -e "${GREEN}Sync complete${NC}"
}

cmd_build() {
    ensure_vm_running
    echo -e "${CYAN}Building FluentPDF...${NC}"
    vagrant ssh -c "powershell -ExecutionPolicy Bypass -File C:\\fluentpdf\\vagrant\\scripts\\build-and-test.ps1 -Build"
}

cmd_test() {
    ensure_vm_running
    echo -e "${CYAN}Running unit tests...${NC}"
    vagrant ssh -c "powershell -ExecutionPolicy Bypass -File C:\\fluentpdf\\vagrant\\scripts\\build-and-test.ps1 -Test"
}

cmd_smoke() {
    ensure_vm_running
    echo -e "${CYAN}Running smoke test...${NC}"
    vagrant ssh -c "powershell -ExecutionPolicy Bypass -File C:\\fluentpdf\\vagrant\\scripts\\build-and-test.ps1 -Smoke"
}

cmd_e2e() {
    ensure_vm_running
    echo -e "${YELLOW}E2E tests require a GUI session.${NC}"
    echo -e "${YELLOW}Connect via RDP first: ./run-tests.sh rdp${NC}"
    echo
    echo -e "${CYAN}Running E2E tests...${NC}"
    vagrant ssh -c "powershell -ExecutionPolicy Bypass -File C:\\fluentpdf\\vagrant\\scripts\\build-and-test.ps1 -E2E"
}

cmd_all() {
    ensure_vm_running
    echo -e "${CYAN}Running full workflow: sync + build + test + smoke${NC}"
    echo

    cmd_sync
    echo

    echo -e "${CYAN}Building and testing...${NC}"
    vagrant ssh -c "powershell -ExecutionPolicy Bypass -File C:\\fluentpdf\\vagrant\\scripts\\build-and-test.ps1 -All"
}

cmd_ssh() {
    ensure_vm_running
    vagrant ssh
}

cmd_rdp() {
    ensure_vm_running
    echo -e "${CYAN}Opening RDP connection...${NC}"
    echo -e "${YELLOW}Credentials: vagrant/vagrant${NC}"

    if command -v xfreerdp &> /dev/null; then
        xfreerdp /u:vagrant /p:vagrant /v:localhost:23389 /dynamic-resolution &
    elif command -v rdesktop &> /dev/null; then
        rdesktop -u vagrant -p vagrant localhost:23389 &
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        open "rdp://vagrant:vagrant@localhost:23389"
    else
        echo -e "${YELLOW}No RDP client found. Install xfreerdp:${NC}"
        echo "  sudo apt-get install freerdp2-x11"
        echo
        echo "Or connect manually to localhost:23389 (vagrant/vagrant)"
    fi
}

cmd_halt() {
    echo -e "${CYAN}Stopping VM...${NC}"
    vagrant halt
    echo -e "${GREEN}VM stopped${NC}"
}

cmd_destroy() {
    echo -e "${RED}This will destroy the VM and all its data.${NC}"
    read -p "Are you sure? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        vagrant destroy -f
        echo -e "${GREEN}VM destroyed${NC}"
    fi
}

# Main
print_header
check_vagrant

case "${1:-}" in
    up)      cmd_up ;;
    sync)    cmd_sync ;;
    build)   cmd_build ;;
    test)    cmd_test ;;
    smoke)   cmd_smoke ;;
    e2e)     cmd_e2e ;;
    all)     cmd_all ;;
    ssh)     cmd_ssh ;;
    rdp)     cmd_rdp ;;
    halt)    cmd_halt ;;
    destroy) cmd_destroy ;;
    help|-h|--help)
        print_usage
        ;;
    *)
        if [ -n "${1:-}" ]; then
            echo -e "${RED}Unknown command: $1${NC}"
            echo
        fi
        print_usage
        exit 1
        ;;
esac
