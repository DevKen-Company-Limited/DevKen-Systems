export interface User {
    roles: any;
    isSuperAdmin: boolean;
    id: string;
    name: string;
    email: string;
    avatar?: string;
    status?: string;
}
