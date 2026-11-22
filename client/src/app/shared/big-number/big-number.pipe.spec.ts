import { BigNumberPipe } from './big-number.pipe';

describe('BigNumberPipe', () => {
  const pipe = new BigNumberPipe();

  it('formats 20! exactly, with thousands separators', () => {
    expect(pipe.transform(2_432_902_008_176_640_000n)).toBe('2,432,902,008,176,640,000');
  });

  it('renders nothing for missing values', () => {
    expect(pipe.transform(null)).toBe('');
  });
});
