# Blazor WASM Cache Strategy

## Problem

Blazor WebAssembly apps use content-based hashing for their framework files (`.wasm`, `.dll`, `.dat`). This means:
- Files like `System.Private.CoreLib.7ujyl7zuo2.wasm` have a hash in their filename
- When you deploy a new version, the hash changes (e.g., `System.Private.CoreLib.abc123xyz.wasm`)
- The boot manifest (`blazor.boot.json`) tells the browser which files to load

**If the browser caches the boot manifest**, it will try to load old files that no longer exist → **404 errors**.

## Solution

### 1. Cache Headers (Nginx)

Our `nginx.conf` implements a **two-tier caching strategy**:

#### ❌ NEVER CACHE (Boot/Manifest Files)
These files must **always** be fetched fresh:
- `index.html`
- `/_framework/blazor.boot.json`
- `/_framework/blazor.webassembly.js`
- `/_framework/dotnet.js` (.NET 8+)
- `/_framework/dotnet.native.js` (.NET 8+)

```nginx
Cache-Control: no-cache, no-store, must-revalidate
Pragma: no-cache
Expires: 0
```

#### ✅ CACHE FOREVER (Hashed Files)
These files have content hashes in their names, so they can be cached indefinitely:
- `/_framework/*.wasm`
- `/_framework/*.dll`
- `/_framework/*.dat`
- `/_content/*` (Blazor component libraries)
- `/css/*`, `/js/*` (if versioned)

```nginx
Cache-Control: public, max-age=31536000, immutable
```

### 2. SPA Fallback Protection

**CRITICAL**: The `/_framework/` and `/_content/` paths must **NEVER** fall back to `index.html`.

```nginx
# WRONG - causes 404s to return HTML instead of proper 404
location / {
    try_files $uri $uri/ /index.html;  # This catches EVERYTHING including /_framework/
}

# CORRECT - framework files return 404 if missing
location /_framework/ {
    try_files $uri =404;  # Return 404 if file doesn't exist
    expires 1y;
    add_header Cache-Control "public, max-age=31536000, immutable";
}

location / {
    try_files $uri $uri/ /index.html;  # Only SPA routes fall back
}
```

### 3. Atomic Deployment

Our `deploy.sh` uses **rolling updates** to ensure:
1. Old files aren't deleted before new ones are in place
2. Containers are recreated atomically (one at a time)
3. No "mixed version" state where some files are old and some are new

```bash
# Update services one by one
docker-compose up -d --no-deps --force-recreate fakturus-track-api
docker-compose up -d --no-deps --force-recreate fakturus-track-ui
```

### 4. No Service Worker (PWA)

We **do NOT use** a Service Worker / PWA setup because:
- Service Workers aggressively cache files
- They can cause "stuck between versions" issues
- They require complex versioning logic
- For our use case, standard HTTP caching is sufficient

## Testing Cache Behavior

### Check if boot files are cached:
```bash
# Should return: Cache-Control: no-cache, no-store, must-revalidate
curl -I https://track.fakturus.com/_framework/blazor.boot.json

# Should return: Cache-Control: public, max-age=31536000, immutable
curl -I https://track.fakturus.com/_framework/System.Private.CoreLib.*.wasm
```

### Force cache clear for testing:
1. Open DevTools → Network tab
2. Check "Disable cache"
3. Application → Clear storage → Clear site data
4. Hard reload: Ctrl+Shift+R (Windows) / Cmd+Shift+R (Mac)

### Check for 404s:
1. Open DevTools → Network tab
2. Filter by "404"
3. If you see `*.wasm` or `*.dll` files with 404, you have a cache/version mismatch

## Deployment Checklist

Before deploying:
- [ ] Build new Docker images with unique tags
- [ ] Ensure nginx.conf has correct cache headers
- [ ] Use rolling update strategy (not `docker-compose down && up`)
- [ ] Wait for health checks before declaring success
- [ ] Monitor for 404 errors in browser console

After deploying:
- [ ] Test with hard reload (Ctrl+Shift+R)
- [ ] Check Network tab for 404 errors
- [ ] Verify `blazor.boot.json` is not cached (304 with no-cache is OK, but should revalidate)
- [ ] Verify framework files return 200 or 304 with long cache

## Common Issues

### Issue: 404 on `*.wasm` files after deployment
**Cause**: Browser cached old `blazor.boot.json`
**Fix**: Clear browser cache, ensure boot files have `no-cache` headers

### Issue: Changes not visible after deployment
**Cause**: `index.html` or boot files are cached
**Fix**: Ensure `index.html` has `no-cache` headers, hard reload

### Issue: Mixed version state during deployment
**Cause**: Non-atomic deployment (files replaced while clients are loading)
**Fix**: Use rolling updates, don't delete old files immediately

## References

- [Blazor WASM Hosting and Deployment](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)
- [HTTP Caching](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)
- [Nginx Location Priority](http://nginx.org/en/docs/http/ngx_http_core_module.html#location)

