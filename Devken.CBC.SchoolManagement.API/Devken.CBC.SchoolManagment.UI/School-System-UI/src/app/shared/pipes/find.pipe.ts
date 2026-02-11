// app/shared/pipes/find.pipe.ts

import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'find',
  standalone: true
})
export class FindPipe implements PipeTransform {
  transform(array: any[], id: any, idKey: string = 'id'): any {
    if (!array || !id) {
      return null;
    }
    return array.find(item => item[idKey] === id);
  }
}