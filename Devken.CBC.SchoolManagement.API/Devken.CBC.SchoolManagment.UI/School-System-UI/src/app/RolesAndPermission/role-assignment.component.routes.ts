import { Component } from "@angular/core";
import { RoleAssignmentEnhancedComponent } from "./role-assignment.component";
import { Routes } from "@angular/router";

export default[
 {
        path: '',
        component: RoleAssignmentEnhancedComponent,
        data: {
            title: 'Role Assignment'
        }
    }
] as Routes;