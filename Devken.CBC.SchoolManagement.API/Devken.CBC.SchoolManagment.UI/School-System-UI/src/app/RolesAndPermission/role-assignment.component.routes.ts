import { Component } from "@angular/core";

import { Routes } from "@angular/router";
import { RoleAssignmentManagementComponent } from "./role-assignment.component";

export default[
 {
        path: '',
        component: RoleAssignmentManagementComponent,
        data: {
            title: 'Role Assignment'
        }
    }
] as Routes;