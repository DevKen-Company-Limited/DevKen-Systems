// subjects.routes.ts
import { Routes }                from '@angular/router';
import { SubjectsComponent }     from './list/subjects.component';
import { SubjectFormComponent }  from './form/subject-form.component';
import { SubjectDetailsComponent } from './details/subject-details.component';

export default [
  {
    path: '',
    component: SubjectsComponent,
  },
  {
    path: 'create',
    component: SubjectFormComponent,
    data: { title: 'Create Subject' },
  },
  {
    path: 'edit/:id',
    component: SubjectFormComponent,
    data: { title: 'Edit Subject' },
  },
  {
    path: 'details/:id',
    component: SubjectDetailsComponent,
    data: { title: 'Subject Details' },
  },
] as Routes;