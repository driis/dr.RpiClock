#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/dr.RpiClock.App"
PUBLISH_DIR="$SCRIPT_DIR/publish"
REMOTE_HOST="driis@rpiclock.local"
REMOTE_DIR="/home/driis/rpiclock"

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
echo "=== Deploying to $REMOTE_HOST:$REMOTE_DIR ==="
ssh "$REMOTE_HOST" "mkdir -p $REMOTE_DIR"
scp -r "$PUBLISH_DIR/"* "$REMOTE_HOST:$REMOTE_DIR/"
ssh "$REMOTE_HOST" "chmod +x $REMOTE_DIR/dr.RpiClock.App $REMOTE_DIR/start.sh"

echo ""
echo "=== Deploy complete ==="
echo "SSH into $REMOTE_HOST and run: $REMOTE_DIR/start.sh"
