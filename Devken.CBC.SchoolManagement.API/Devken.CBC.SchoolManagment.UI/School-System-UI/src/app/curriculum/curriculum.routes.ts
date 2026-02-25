import { Routes } from '@angular/router';
import { LearningAreaFormComponent } from '../dialog-modals/Curriculum/learning-area-form/learning-area-form.component';
import { LearningAreasComponent } from './learning-areas/learning-areas.component';
import { LearningOutcomeFormComponent } from '../dialog-modals/Curriculum/learning-outcome-form/learning-outcome-form.component';
import { LearningOutcomesComponent } from './learning-outcome/learning-outcomes.component';
import { StrandFormComponent } from '../dialog-modals/Curriculum/strand-form/strand-form.component';
import { StrandsComponent } from './strands/strands.component';
import { SubStrandFormComponent } from '../dialog-modals/Curriculum/sub-strand-form/sub-strand-form.component';
import { SubStrandsComponent } from './sub-strands/sub-strands.component';

export default [
  // Learning Areas
  {
    path: 'learning-areas',
    component: LearningAreasComponent,
    data: { title: 'Learning Areas', breadcrumb: 'Learning Areas' },
  },
  {
    path: 'learning-areas/create',
    component: LearningAreaFormComponent,
    data: { title: 'Create Learning Area' },
  },
  {
    path: 'learning-areas/edit/:id',
    component: LearningAreaFormComponent,
    data: { title: 'Edit Learning Area' },
  },
  // Strands
  {
    path: 'strands',
    component: StrandsComponent,
    data: { title: 'Strands', breadcrumb: 'Strands' },
  },
  {
    path: 'strands/create',
    component: StrandFormComponent,
    data: { title: 'Create Strand' },
  },
  {
    path: 'strands/edit/:id',
    component: StrandFormComponent,
    data: { title: 'Edit Strand' },
  },
  // Sub-Strands
  {
    path: 'sub-strands',
    component: SubStrandsComponent,
    data: { title: 'Sub-Strands', breadcrumb: 'Sub-Strands' },
  },
  {
    path: 'sub-strands/create',
    component: SubStrandFormComponent,
    data: { title: 'Create Sub-Strand' },
  },
  {
    path: 'sub-strands/edit/:id',
    component: SubStrandFormComponent,
    data: { title: 'Edit Sub-Strand' },
  },
  // Learning Outcomes
  {
    path: 'learning-outcomes',
    component: LearningOutcomesComponent,
    data: { title: 'Learning Outcomes', breadcrumb: 'Learning Outcomes' },
  },
  {
    path: 'learning-outcomes/create',
    component: LearningOutcomeFormComponent,
    data: { title: 'Create Learning Outcome' },
  },
  {
    path: 'learning-outcomes/edit/:id',
    component: LearningOutcomeFormComponent,
    data: { title: 'Edit Learning Outcome' },
  },
  {
    path: '',
    redirectTo: 'learning-areas',
    pathMatch: 'full',
  },
] as Routes;