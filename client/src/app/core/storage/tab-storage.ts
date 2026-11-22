const BIGINT_TAG = '$bigint';

function replacer(_key: string, value: unknown): unknown {
  return typeof value === 'bigint' ? { [BIGINT_TAG]: value.toString() } : value;
}

function reviver(_key: string, value: unknown): unknown {
  if (value && typeof value === 'object' && BIGINT_TAG in value) {
    const digits = (value as Record<string, unknown>)[BIGINT_TAG];
    return typeof digits === 'string' && /^-?\d+$/.test(digits) ? BigInt(digits) : null;
  }
  return value;
}

// Returns the stored value, or null when missing, unreadable or malformed.
export function readTabItem(key: string): unknown {
  try {
    const raw = sessionStorage.getItem(key);
    return raw === null ? null : JSON.parse(raw, reviver);
  } catch {
    return null;
  }
}

// Stores the value in sessionStorage; null removes the key.
export function writeTabItem(key: string, value: unknown): void {
  try {
    if (value === null) {
      sessionStorage.removeItem(key);
    } else {
      sessionStorage.setItem(key, JSON.stringify(value, replacer));
    }
  } catch {
  }
}
