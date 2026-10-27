export const HOST = 'com.flowdesk.flowweb';
export function webUrl(value) {
  if (typeof value !== 'string' || value.length > 2048) return false;
  try { const u = new URL(value); return ['http:', 'https:'].includes(u.protocol) && !u.username && !u.password; } catch { return false; }
}
export function packet(type, data, id = crypto.randomUUID().replaceAll('-', '')) { return { v: 1, id, type, data }; }
export function validPacket(p) { return !!p && p.v === 1 && /^[a-f0-9]{32}$/.test(p.id) && typeof p.type === 'string' && p.data && typeof p.data === 'object' && !Array.isArray(p.data); }
export function validCommand(c) {
  if (!c || typeof c !== 'object' || Array.isArray(c)) return false;
  if (['activate', 'mute', 'pin'].includes(c.action)) return Object.keys(c).every(k => ['action', 'tabId', 'value'].includes(k)) && Number.isSafeInteger(c.tabId) && c.tabId > 0 && (c.action === 'activate' ? c.value === undefined : typeof c.value === 'boolean');
  return c.action === 'openWorkspace' && Object.keys(c).every(k => ['action', 'urls'].includes(k)) && Array.isArray(c.urls) && c.urls.length >= 1 && c.urls.length <= 25 && c.urls.every(webUrl);
}
