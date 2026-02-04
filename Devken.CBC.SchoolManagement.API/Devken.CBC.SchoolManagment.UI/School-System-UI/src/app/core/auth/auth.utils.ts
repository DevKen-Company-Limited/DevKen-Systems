// auth.utils.ts
export class AuthUtils {
    // Buffer time in seconds before actual expiry to trigger refresh
    private static readonly EXPIRY_BUFFER_SECONDS = 60; // 1 minute buffer

    static isValidTokenFormat(token: string): boolean {
        if (!token) return false;
        const parts = token.split('.');
        return parts.length === 3 && parts.every(part => part.length > 0);
    }

    static isTokenExpired(token: string, bufferSeconds = 0): boolean {
        if (!token || !this.isValidTokenFormat(token)) {
            return true;
        }

        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const expiry = payload.exp;
            if (!expiry) return true;
            
            const expiryTime = expiry * 1000;
            const currentTime = Date.now();
            const bufferTime = bufferSeconds * 1000;
            
            return currentTime >= (expiryTime - bufferTime);
        } catch {
            return true;
        }
    }

    /**
     * Check if token will expire soon (within buffer time)
     */
    static shouldRefreshToken(token: string): boolean {
        return this.isTokenExpired(token, this.EXPIRY_BUFFER_SECONDS);
    }

    /**
     * Get time until token expires in seconds
     */
    static getTimeUntilExpiry(token: string): number | null {
        if (!token || !this.isValidTokenFormat(token)) {
            return null;
        }

        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const expiry = payload.exp;
            if (!expiry) return null;
            
            const expiryTime = expiry * 1000;
            const currentTime = Date.now();
            const timeLeft = Math.floor((expiryTime - currentTime) / 1000);
            
            return timeLeft > 0 ? timeLeft : 0;
        } catch {
            return null;
        }
    }

    private static getTokenPayload(token: string): any {
        try {
            if (!this.isValidTokenFormat(token)) return null;
            return JSON.parse(atob(token.split('.')[1]));
        } catch {
            return null;
        }
    }

    static getUserIdFromToken(token: string): string | null {
        const payload = this.getTokenPayload(token);
        return payload?.sub || payload?.user_id || payload?.userId || null;
    }

    static getUserEmailFromToken(token: string): string | null {
        const payload = this.getTokenPayload(token);
        return payload?.email || null;
    }

    static getUserNameFromToken(token: string): string | null {
        const payload = this.getTokenPayload(token);
        return payload?.name || payload?.fullName || null;
    }

    static getUserRolesFromToken(token: string): string[] {
        const payload = this.getTokenPayload(token);
        if (!payload) return [];
        
        const roles = payload.role || payload.roles || [];
        return Array.isArray(roles) ? roles : [roles].filter(Boolean);
    }

    static getUserPermissionsFromToken(token: string): string[] {
        const payload = this.getTokenPayload(token);
        if (!payload) return [];
        
        const perms = payload.permission || payload.permissions || [];
        return Array.isArray(perms) ? perms : [perms].filter(Boolean);
    }

    static isSuperAdmin(token: string): boolean {
        const payload = this.getTokenPayload(token);
        if (!payload) return false;
        
        return payload.is_super_admin === true || 
               payload.isSuperAdmin === true ||
               (payload.roles && (
                   payload.roles.includes('SuperAdmin') ||
                   payload.roles.includes('super_admin')
               ));
    }

    static getTenantIdFromToken(token: string): string | null {
        const payload = this.getTokenPayload(token);
        return payload?.tenant_id || payload?.tenantId || null;
    }
}