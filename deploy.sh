#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/dr.RpiClock.App"
PUBLISH_DIR="$SCRIPT_DIR/publish"
REMOTE_HOST="driis@rpiclock.local"
REMOTE_DIR="/home/driis/rpiclock"
SERVICE_NAME="rpiclock"

echo "=== Building for Linux ARM (Raspberry Pi) ==="
dotnet publish "$PROJECT_DIR" \
    --runtime linux-arm64 \
    --self-contained true \
    -c Release \
    -o "$PUBLISH_DIR"

echo ""
echo "=== Creating start.sh ==="
cat > "$PUBLISH_DIR/start.sh" << 'STARTEOF'
#!/bin/bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
./dr.RpiClock.App /dev/fb0 -c
STARTEOF
chmod +x "$PUBLISH_DIR/start.sh"

echo ""
echo "=== Creating systemd service ==="
cat > "$PUBLISH_DIR/$SERVICE_NAME.service" << SERVICEEOF
[Unit]
Description=RpiClock Display
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=$REMOTE_DIR
ExecStart=$REMOTE_DIR/start.sh
Restart=on-failure
RestartSec=5
User=driis

[Install]
WantedBy=multi-user.target
SERVICEEOF

echo ""
echo "=== Stopping service before deploy ==="
ssh "$REMOTE_HOST" "sudo systemctl stop $SERVICE_NAME 2>/dev/null || true"

echo ""
echo "=== Deploying to $REMOTE_HOST:$REMOTE_DIR ==="
ssh "$REMOTE_HOST" "mkdir -p $REMOTE_DIR"
scp -r "$PUBLISH_DIR/"* "$REMOTE_HOST:$REMOTE_DIR/"
ssh "$REMOTE_HOST" "chmod +x $REMOTE_DIR/dr.RpiClock.App $REMOTE_DIR/start.sh"

echo ""
echo "=== Installing and starting systemd service ==="
ssh "$REMOTE_HOST" "sudo cp $REMOTE_DIR/$SERVICE_NAME.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable $SERVICE_NAME && sudo systemctl restart $SERVICE_NAME"

echo ""
echo "=== Deploy complete ==="
echo "Service status: ssh $REMOTE_HOST sudo systemctl status $SERVICE_NAME"
echo "View logs:      ssh $REMOTE_HOST journalctl -u $SERVICE_NAME -f"
