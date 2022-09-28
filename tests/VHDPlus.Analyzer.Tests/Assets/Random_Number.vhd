--
--  Pseudo Random Number Generator "xoshiro128++ 1.0".
--
--  Author: Joris van Rantwijk <joris@jorisvr.nl>
--
--  This is a 32-bit random number generator in synthesizable VHDL.
--  The generator can produce 32 new random bits on every clock cycle.
--
--  The algorithm "xoshiro128++" is by David Blackman and Sebastiano Vigna.
--  See also http://prng.di.unimi.it/
--
--  The generator requires a 128-bit seed value, not equal to all zeros.
--  A default seed must be supplied at compile time and will be used
--  to initialize the generator at reset. The generator also supports
--  re-seeding at run time.
--
--  After reset and after re-seeding, one or two clock cycles are needed
--  before valid random data appears on the output. The exact delay
--  depends on the setting of the "pipeline" parameter.
--
--  NOTE: This is not a cryptographic random number generator.
--

--
--  Copyright (C) 2020 Joris van Rantwijk
--
--  This code is free software; you can redistribute it and/or
--  modify it under the terms of the GNU Lesser General Public
--  License as published by the Free Software Foundation; either
--  version 2.1 of the License, or (at your option) any later version.
--
--  See <https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html>
--

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;


entity Random_Number is

    generic (
        -- Default seed value.
        init_seed:  std_logic_vector(127 downto 0) := x"0123456789abcdef0123456789abcdef";

        -- Enable optional pipeline stage in output calculation.
        -- This uses an extra 32-bit register but tends to improve
        -- the timing performance of the circuit.
        -- If the pipeline stage is enabled, two clock cycles are needed
        -- before valid output appears after reset and after re-seeding.
        -- If the pipeline stage is disabled, just one clock cycle is needed.
        pipeline:   boolean := true );

    port (

        -- Clock, rising edge active.
        clk:        in  std_logic;  );

end entity;

