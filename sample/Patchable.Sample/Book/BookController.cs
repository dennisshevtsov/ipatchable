﻿// Copyright (c) Dennis Shevtsov. All rights reserved.
// Licensed under the MIT License.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace Patchable.Sample.Book;

[Route("book")]
[ApiController]
public sealed class BookController : ControllerBase
{
  private BookService _bookService;

  public BookController(BookService bookService)
  {
    _bookService = bookService ?? throw new ArgumentNullException(nameof(bookService));
  }

  [HttpGet("{bookId}", Name = nameof(BookController.GetBook))]
  public IActionResult GetBook(GetBookRequestDto requestDto)
  {
    BookEntity? bookEntity = _bookService.GetBook(requestDto.BookId);

    if (bookEntity == null)
    {
      return NotFound();
    }

    return Ok(new GetBookResponseDto(bookEntity));
  }

  [HttpPost(Name = nameof(BookController.PostBook))]
  public IActionResult PostBook(PostBookRequestDto requestDto)
  {
    BookEntity bookEntity = requestDto.ToBookEntity();
    _bookService.SaveBook(bookEntity);

    return CreatedAtRoute(nameof(BookController.GetBook), new GetBookResponseDto(bookEntity));
  }

  [HttpPut("{bookId}", Name = nameof(PutBook))]
  public IActionResult PutBook(PutBookRequestDto requestDto)
  {
    return Ok();
  }

  [HttpPatch("{bookId}", Name = nameof(PatchBook))]
  public IActionResult PatchBook(PatchBookRequestDto requestDto)
  {
    return Ok();
  }

  [HttpDelete("{bookId}", Name = nameof(DeleteBook))]
  public IActionResult DeleteBook(DeleteBookRequestDto requestDto)
  {
    return Ok();
  }
}
