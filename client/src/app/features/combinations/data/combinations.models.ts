export interface StartResponseDto {
  sessionId: string;
  n: number;
  totalCount: string;
}

export interface CombinationDto {
  ordinal: string;
  values: number[];
}

export interface NextResponseDto {
  combination: CombinationDto | null;
  hasMore: boolean;
  totalCount: string;
  message: string | null;
}

export interface GetAllRequestDto {
  pageSize: number;
  pageNumber: string;
  anchor?: string;
}

export interface PageResponseDto {
  items: CombinationDto[];
  anchor: string;
  pageNumber: string;
  pageSize: number;
  totalPages: string;
  totalItems: string;
  totalCount: string;
  message: string | null;
}

export interface Combination {
  readonly ordinal: bigint;
  readonly values: readonly number[];
}

export interface GenerationSession {
  readonly id: string;
  readonly n: number;
  readonly totalCount: bigint;
}

export interface NextResult {
  readonly combination: Combination | null;
  readonly hasMore: boolean;
  readonly message: string | null;
}

export interface CombinationPage {
  readonly items: readonly Combination[];
  readonly anchor: bigint;
  readonly pageNumber: bigint;
  readonly pageSize: number;
  readonly totalPages: bigint;
  readonly totalItems: bigint;
  readonly message: string | null;
}

export interface PageRequest {
  readonly pageSize: number;
  readonly pageNumber: bigint;
  readonly anchor?: bigint;
}

export const toCombination = (dto: CombinationDto): Combination => ({
  ordinal: BigInt(dto.ordinal),
  values: dto.values,
});

export const toSession = (dto: StartResponseDto): GenerationSession => ({
  id: dto.sessionId,
  n: dto.n,
  totalCount: BigInt(dto.totalCount),
});

export const toNextResult = (dto: NextResponseDto): NextResult => ({
  combination: dto.combination ? toCombination(dto.combination) : null,
  hasMore: dto.hasMore,
  message: dto.message,
});

export const toPage = (dto: PageResponseDto): CombinationPage => ({
  items: dto.items.map(toCombination),
  anchor: BigInt(dto.anchor),
  pageNumber: BigInt(dto.pageNumber),
  pageSize: dto.pageSize,
  totalPages: BigInt(dto.totalPages),
  totalItems: BigInt(dto.totalItems),
  message: dto.message,
});

export const toGetAllRequest = (request: PageRequest): GetAllRequestDto => ({
  pageSize: request.pageSize,
  pageNumber: request.pageNumber.toString(),
  ...(request.anchor !== undefined && { anchor: request.anchor.toString() }),
});
