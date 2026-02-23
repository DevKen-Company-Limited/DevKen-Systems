// assessment-review-step/assessment-review-step.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule }    from '@angular/common';
import { MatCardModule }   from '@angular/material/card';
import { MatIconModule }   from '@angular/material/icon';
import { FuseAlertComponent } from '@fuse/components/alert';
import {
  AssessmentType,
  AssessmentMethod,
  AssessmentTypeOptions,
  getAssessmentTypeLabel,
  getAssessmentTypeColor,
} from '../types/AssessmentDtos';


// ── Helper: human-readable label for AssessmentMethod ─────────────
function getAssessmentMethodLabel(method: string): string {
  const map: Record<string, string> = {
    [AssessmentMethod.Observation]:    'Observation',
    [AssessmentMethod.Portfolio]:      'Portfolio',
    [AssessmentMethod.Project]:        'Project',
    [AssessmentMethod.Practical]:      'Practical',
    [AssessmentMethod.Written]:        'Written',
    [AssessmentMethod.Oral]:           'Oral',
    [AssessmentMethod.PeerAssessment]: 'Peer Assessment',
  };
  return map[method] ?? method;
}

export interface AssessmentEnrollmentStep {
  label:      string;
  icon:       string;
  sectionKey: string;
}

@Component({
  selector: 'app-assessment-review-step',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, FuseAlertComponent],
  templateUrl: './assessment-review-step.component.html',
})
export class AssessmentReviewStepComponent {

  @Input() formSections:  Record<string, any>     = {};
  @Input() steps:         AssessmentEnrollmentStep[] = [];
  @Input() completedSteps = new Set<number>();
  @Input() isSuperAdmin   = false;
  @Output() editSection   = new EventEmitter<number>();

  readonly AssessmentType = AssessmentType;

  getTypeName   = getAssessmentTypeLabel;
  getTypeColor  = getAssessmentTypeColor;
  getMethodName = getAssessmentMethodLabel;

  // ── Section accessors ──────────────────────────────────────────────
  get typeStep(): any     { return this.formSections['type']     ?? {}; }
  get identity(): any     { return this.formSections['identity'] ?? {}; }
  get details():  any     { return this.formSections['details']  ?? {}; }

  get assessmentType(): AssessmentType {
    return this.typeStep.assessmentType ?? AssessmentType.Formative;
  }

  get typeOption() {
    return AssessmentTypeOptions.find(o => o.value === this.assessmentType);
  }

  // ── Completion helpers ─────────────────────────────────────────────
  isComplete(index: number): boolean { return this.completedSteps.has(index); }

  completedCount(): number {
    return Array.from(this.completedSteps)
      .filter(i => i < this.steps.length - 1).length;
  }

  allComplete(): boolean {
    return this.completedCount() === this.steps.length - 1;
  }

  getCompletionPct(): number {
    if (this.steps.length <= 1) return 0;
    return Math.round((this.completedCount() / (this.steps.length - 1)) * 100);
  }

  // ── Type-specific section visibility ──────────────────────────────
  get isFormative():  boolean { return this.assessmentType === AssessmentType.Formative;  }
  get isSummative():  boolean { return this.assessmentType === AssessmentType.Summative;  }
  get isCompetency(): boolean { return this.assessmentType === AssessmentType.Competency; }

  // ── Formatted review fields ────────────────────────────────────────

  /** Core identity fields shown for every type */
  get coreFields(): { label: string; value: string; icon: string }[] {
    const id = this.identity;
    const fields = [
      { label: 'Title',          value: this.field(id, 'title'),          icon: 'title'         },
      { label: 'Description',    value: this.field(id, 'description'),     icon: 'notes'         },
      { label: 'Teacher',        value: this.field(id, 'teacherId'),       icon: 'person'        },
      { label: 'Subject',        value: this.field(id, 'subjectId'),       icon: 'menu_book'     },
      { label: 'Class',          value: this.field(id, 'classId'),         icon: 'class'         },
      { label: 'Term',           value: this.field(id, 'termId'),          icon: 'date_range'    },
      { label: 'Academic Year',  value: this.field(id, 'academicYearId'),  icon: 'school'        },
      { label: 'Date',           value: this.formatDate(id.assessmentDate), icon: 'event'        },
      { label: 'Maximum Score',  value: this.field(id, 'maximumScore'),    icon: 'grade'         },
      { label: 'Status',         value: id.isPublished ? 'Published' : 'Draft', icon: 'public'  },
    ];
    // SuperAdmin only
    if (this.isSuperAdmin && id.schoolId) {
      fields.unshift({ label: 'School', value: this.field(id, 'schoolId'), icon: 'apartment' });
    }
    return fields.filter(f => f.value !== '—');
  }

  get formativeFields(): { label: string; value: string; icon: string }[] {
    const d = this.details;
    return [
      { label: 'Formative Type',    value: this.field(d, 'formativeType'),          icon: 'assignment'   },
      { label: 'Competency Area',   value: this.field(d, 'competencyArea'),          icon: 'hub'          },
      { label: 'Learning Outcome',  value: this.field(d, 'learningOutcomeId'),       icon: 'flag'         },
      { label: 'Strand',            value: this.field(d, 'formativeStrand'),         icon: 'account_tree' },
      { label: 'Sub-Strand',        value: this.field(d, 'formativeSubStrand'),      icon: 'device_hub'   },
      { label: 'Weight',            value: d.assessmentWeight != null ? `${d.assessmentWeight}%` : '—', icon: 'percent' },
      { label: 'Requires Rubric',   value: d.requiresRubric != null ? (d.requiresRubric ? 'Yes' : 'No') : '—', icon: 'table_chart' },
      { label: 'Criteria',          value: this.field(d, 'criteria'),                icon: 'checklist'    },
      { label: 'Feedback Template', value: this.field(d, 'feedbackTemplate'),        icon: 'rate_review'  },
      { label: 'Instructions',      value: this.field(d, 'formativeInstructions'),   icon: 'notes'        },
    ].filter(f => f.value !== '—');
  }

  get summativeFields(): { label: string; value: string; icon: string }[] {
    const d = this.details;
    return [
      { label: 'Exam Type',        value: this.field(d, 'examType'),                                           icon: 'assignment_late'      },
      { label: 'Duration',         value: this.field(d, 'duration'),                                           icon: 'timer'                },
      { label: 'Questions',        value: this.field(d, 'numberOfQuestions'),                                  icon: 'format_list_numbered' },
      { label: 'Pass Mark',        value: d.passMark != null ? `${d.passMark}%` : '—',                         icon: 'percent'              },
      { label: 'Has Practical',    value: d.hasPracticalComponent != null ? (d.hasPracticalComponent ? 'Yes' : 'No') : '—', icon: 'science' },
      { label: 'Practical Weight', value: d.hasPracticalComponent ? `${d.practicalWeight}%` : '—',            icon: 'percent'              },
      { label: 'Theory Weight',    value: d.hasPracticalComponent ? `${d.theoryWeight}%` : '—',               icon: 'percent'              },
      { label: 'Instructions',     value: this.field(d, 'summativeInstructions'),                              icon: 'notes'                },
    ].filter(f => f.value !== '—');
  }

  get competencyFields(): { label: string; value: string; icon: string }[] {
    const d = this.details;
    return [
      { label: 'Competency Name',   value: this.field(d, 'competencyName'),                                              icon: 'verified_user' },
      { label: 'Strand',            value: this.field(d, 'competencyStrand'),                                            icon: 'account_tree'  },
      { label: 'Sub-Strand',        value: this.field(d, 'competencySubStrand'),                                         icon: 'device_hub'    },
      { label: 'Assessment Method', value: d.assessmentMethod ? this.getMethodName(d.assessmentMethod) : '—',            icon: 'visibility'    },
      { label: 'Rating Scale',      value: this.field(d, 'ratingScale'),                                                 icon: 'stars'         },
      { label: 'Observation Based', value: d.isObservationBased != null ? (d.isObservationBased ? 'Yes' : 'No') : '—',  icon: 'visibility'    },
      { label: 'Tools Required',    value: this.field(d, 'toolsRequired'),                                               icon: 'build'         },
      { label: 'SLO',               value: this.field(d, 'specificLearningOutcome'),                                     icon: 'flag'          },
      { label: 'Perf. Indicators',  value: this.field(d, 'performanceIndicators'),                                       icon: 'checklist'     },
      { label: 'Instructions',      value: this.field(d, 'competencyInstructions'),                                      icon: 'notes'         },
    ].filter(f => f.value !== '—');
  }

  get typeSpecificFields(): { label: string; value: string; icon: string }[] {
    switch (this.assessmentType) {
      case AssessmentType.Formative:  return this.formativeFields;
      case AssessmentType.Summative:  return this.summativeFields;
      case AssessmentType.Competency: return this.competencyFields;
      default: return [];
    }
  }

  // ── Utilities ──────────────────────────────────────────────────────
  formatDate(val: any): string {
    if (!val) return '—';
    try {
      const d = typeof val === 'string' ? new Date(val) : val as Date;
      return isNaN(d.getTime()) ? '—'
        : d.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
    } catch { return '—'; }
  }

  field(obj: any, key: string, fallback = '—'): string {
    const v = obj?.[key];
    return (v !== null && v !== undefined && v !== '') ? String(v) : fallback;
  }

  trackByLabel(_index: number, item: { label: string }): string { return item.label; }
}