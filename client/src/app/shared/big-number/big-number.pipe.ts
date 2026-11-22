import { Pipe, PipeTransform } from '@angular/core';

// Formats a bigint with thousands separators without losing precision.
@Pipe({ name: 'bigNumber' })
export class BigNumberPipe implements PipeTransform {
  transform(value: bigint | null | undefined): string {
    return value === null || value === undefined ? '' : value.toLocaleString('en-US');
  }
}
