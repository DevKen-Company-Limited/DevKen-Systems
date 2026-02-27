import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector     : 'example',
    standalone   : true,
    imports      : [CommonModule],
    templateUrl  : './example.component.html',
    styleUrls    : ['./example.component.scss'],
    encapsulation: ViewEncapsulation.None,
})
export class ExampleComponent
{
    activeLevel = 'All Levels';

    levels = ['All Levels','PP1','PP2','Grade 1','Grade 2','Grade 3','Grade 4','Grade 5','Grade 6','JHS 1','JHS 2','JHS 3'];

    stats = [
        { icon:'👥', value:'1,248', label:'Enrolled Students', trend:'↑ 3.2% from last term', color:'#2563EB', iconBg:'rgba(37,99,235,0.1)', trendBg:'rgba(22,163,74,0.1)', trendColor:'#16a34a' },
        { icon:'🎓', value:'84',    label:'Teaching Staff',    trend:'↑ 2 new this term',     color:'#16a34a', iconBg:'rgba(22,163,74,0.1)',  trendBg:'rgba(22,163,74,0.1)', trendColor:'#16a34a' },
        { icon:'📋', value:'312',   label:'Assessments Pending', trend:'↑ 12 overdue',        color:'#d97706', iconBg:'rgba(217,119,6,0.1)', trendBg:'rgba(220,38,38,0.1)', trendColor:'#dc2626' },
        { icon:'💰', value:'78%',   label:'Fee Collection Rate', trend:'↓ 4% target gap',    color:'#dc2626', iconBg:'rgba(220,38,38,0.1)', trendBg:'rgba(220,38,38,0.1)', trendColor:'#dc2626' },
    ];

    classes = [
        { badge:'G6A',  name:'Grade 6A',  meta:'42 students · Mrs. Achieng', color:'#2563EB', pct:88 },
        { badge:'G5B',  name:'Grade 5B',  meta:'38 students · Mr. Kamau',    color:'#16a34a', pct:82 },
        { badge:'G4A',  name:'Grade 4A',  meta:'40 students · Ms. Wanjiku',  color:'#7c3aed', pct:75 },
        { badge:'G3B',  name:'Grade 3B',  meta:'35 students · Mr. Omondi',   color:'#d97706', pct:68 },
        { badge:'PP2A', name:'PP2A',      meta:'30 students · Mrs. Njeri',   color:'#0891b2', pct:91 },
        { badge:'JHS1', name:'JHS 1',     meta:'44 students · Mr. Mutua',    color:'#dc2626', pct:71 },
    ];

    competencies = [
        { label:'Exceeding Expectations',  color:'#2563EB', pct:24 },
        { label:'Meeting Expectations',    color:'#16a34a', pct:48 },
        { label:'Approaching Expectations',color:'#d97706', pct:19 },
        { label:'Below Expectations',      color:'#dc2626', pct:9  },
    ];

    quickActions = [
        { icon:'📝', label:'Record Assessment' },
        { icon:'📊', label:'Generate Report'   },
        { icon:'👤', label:'Add Student'        },
        { icon:'📚', label:'Lesson Plan'        },
    ];

    assessments = [
        { student:'Amina Hassan',   class:'G5B',  area:'Mathematics',  type:'Formative', typeBg:'rgba(99,102,241,.1)',  typeColor:'#6366f1', level:'EE', levelBg:'rgba(37,99,235,0.1)',  levelColor:'#2563EB', date:'14 Mar 2025' },
        { student:'Brian Otieno',   class:'G6A',  area:'Languages',    type:'Summative', typeBg:'rgba(168,85,247,.1)', typeColor:'#a855f7', level:'ME', levelBg:'rgba(22,163,74,0.1)',  levelColor:'#16a34a', date:'13 Mar 2025' },
        { student:'Cynthia Waweru', class:'G4A',  area:'Environmental',type:'Formative', typeBg:'rgba(99,102,241,.1)',  typeColor:'#6366f1', level:'AE', levelBg:'rgba(217,119,6,0.1)', levelColor:'#d97706', date:'12 Mar 2025' },
        { student:'Daniel Kibet',   class:'G3B',  area:'Psychomotor',  type:'Formative', typeBg:'rgba(99,102,241,.1)',  typeColor:'#6366f1', level:'BE', levelBg:'rgba(220,38,38,0.1)', levelColor:'#dc2626', date:'11 Mar 2025' },
        { student:'Esther Muthoni', class:'JHS1', area:'Mathematics',  type:'Summative', typeBg:'rgba(168,85,247,.1)', typeColor:'#a855f7', level:'ME', levelBg:'rgba(22,163,74,0.1)',  levelColor:'#16a34a', date:'10 Mar 2025' },
    ];

    events = [
        { day:'18', month:'Mar', title:'Term 1 Summative Assessments', sub:'All classes · Grade 4–6', tag:'Assessment', tagBg:'rgba(37,99,235,0.1)',  tagColor:'#2563EB' },
        { day:'25', month:'Mar', title:'Progress Reports Due',          sub:'Submit to admin by 5 PM',  tag:'Report',     tagBg:'rgba(22,163,74,0.1)',  tagColor:'#16a34a' },
        { day:'28', month:'Mar', title:'Parent-Teacher Meeting',        sub:'All teachers required',    tag:'Meeting',    tagBg:'rgba(217,119,6,0.1)', tagColor:'#d97706' },
    ];

    finance = [
        { label:'Expected (Term 1)', value:'KSh 4.2M',    color:'inherit'  },
        { label:'Collected',          value:'KSh 3.28M',   color:'#16a34a'  },
        { label:'Outstanding',        value:'KSh 924K',    color:'#dc2626'  },
        { label:'Defaulters',         value:'274 students',color:'#dc2626'  },
    ];

    setActiveLevel(level: string): void
    {
        this.activeLevel = level;
    }

    /**
     * Constructor
     */
    constructor()
    {
    }
}