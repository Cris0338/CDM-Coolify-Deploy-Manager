#!/usr/bin/env bash
# CDM 3.0.0 — ONE-TIME root bootstrap. Public key is not a secret.
# Strictly validated account, public key and port placeholders are substituted by CDM.
set -euo pipefail
new_user='__CDM_USER__'
public_key='__CDM_PUBLIC_KEY__'
coolify_port='__CDM_PORT__'
destination="127.0.0.1:$coolify_port"
config=/etc/ssh/sshd_config
sshd_bin="$(command -v sshd || true)"
if [ -z "$sshd_bin" ]; then sshd_bin=/usr/sbin/sshd; fi
[ "$(id -u)" -eq 0 ] || { echo 'CDM_ERROR: administrator privileges required' >&2; exit 21; }
[ -x "$sshd_bin" ] && [ -f "$config" ] || { echo 'CDM_ERROR: sshd unavailable' >&2; exit 22; }
[ -z "$(getent passwd "$new_user" || true)" ] || { echo 'CDM_ERROR: account already exists; no overwrite' >&2; exit 23; }
# Refuse dangerous global overrides; do not weaken an existing system-wide policy.
"$sshd_bin" -t
backup="$(mktemp /etc/ssh/.cdm-sshd-config.XXXXXX)"
cp -p "$config" "$backup"
created=0
finished=0
reload_ssh() {
  if command -v systemctl >/dev/null 2>&1; then
    systemctl reload ssh 2>/dev/null || systemctl reload sshd 2>/dev/null
  else
    service ssh reload 2>/dev/null || service sshd reload 2>/dev/null
  fi
}
cleanup() {
  result=$?
  trap - EXIT
  if [ "$finished" -ne 1 ]; then
    cp -p "$backup" "$config" || true
    "$sshd_bin" -t >/dev/null 2>&1 && reload_ssh >/dev/null 2>&1 || true
    if [ "$created" -eq 1 ]; then userdel -r "$new_user" >/dev/null 2>&1 || true; fi
  fi
  rm -f "$backup"
  exit "$result"
}
trap cleanup EXIT
useradd --create-home --user-group --shell /usr/sbin/nologin "$new_user"
created=1
# Verify that the new account has not inherited a privileged supplementary group.
for forbidden in sudo wheel docker adm lxd; do
  if id -nG "$new_user" | tr ' ' '\n' | grep -qxF "$forbidden"; then
    echo "CDM_ERROR: service user inherited privileged group $forbidden" >&2
    exit 27
  fi
done
home_dir="$(getent passwd "$new_user" | cut -d: -f6)"
[ -n "$home_dir" ] && [ -d "$home_dir" ] || exit 24
install -d -m 700 -o "$new_user" -g "$new_user" "$home_dir/.ssh"
# Key restrictions PLUS Match User: no PTY, login, exec or arbitrary forwards.
printf 'restrict,port-forwarding,permitopen="%s",command="/bin/false" %s\n' "$destination" "$public_key" > "$home_dir/.ssh/authorized_keys"
chown "$new_user:$new_user" "$home_dir/.ssh/authorized_keys"
chmod 600 "$home_dir/.ssh/authorized_keys"
cat >> "$config" <<CDM_MATCH

# CDM v3 dedicated tunnel account. Do not change administrator accounts.
Match User $new_user
    PasswordAuthentication no
    KbdInteractiveAuthentication no
    PubkeyAuthentication yes
    AuthenticationMethods publickey
    MaxSessions 0
    ForceCommand /bin/false
    PermitTTY no
    AllowTcpForwarding local
    PermitOpen $destination
    AllowStreamLocalForwarding no
    AllowAgentForwarding no
    X11Forwarding no
    PermitTunnel no
    PermitUserRC no
CDM_MATCH
"$sshd_bin" -t
effective="$("$sshd_bin" -T -C "user=$new_user,host=localhost,addr=127.0.0.1")"
for required in "maxsessions 0" "allowtcpforwarding local" "permitopen $destination" "allowstreamlocalforwarding no" "passwordauthentication no" "forcecommand /bin/false" "permittty no"; do
  printf '%s\n' "$effective" | grep -qxF "$required" || {
    echo "CDM_ERROR: sshd effective policy failed: $required" >&2; exit 25;
  }
done
reload_ssh || { echo 'CDM_ERROR: ssh reload failed, rolling back' >&2; exit 26; }
finished=1
echo "CDM_BOOTSTRAP_OK:$new_user"
