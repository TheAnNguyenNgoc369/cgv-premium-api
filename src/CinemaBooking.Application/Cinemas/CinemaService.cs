using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Cinemas;

public sealed class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _cinemaRepository;

    public CinemaService(ICinemaRepository cinemaRepository)
    {
        _cinemaRepository = cinemaRepository;
    }

    public async Task<List<Cinema>> GetActiveCinemasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _cinemaRepository.GetActiveCinemasAsync(cancellationToken);
    }
}